using System.Reflection;
using System.Text.RegularExpressions;

namespace LeesBybel;

/// <summary>A book as references know it: <see cref="One"/> is the name of one chapter's worth,
/// "Psalm" where the book's name is "Psalms". <see cref="Verses"/> has each chapter's last verse.
/// <see cref="Short"/> is its short name ("1 Kor"); <see cref="Number"/> its place, from 1.</summary>
public sealed record BookName(string Code, string Name, string One, int[] Verses,
    string NameKey, string AbbrKey, string[] AliasKeys, string Short, int Number);

/// <summary>A reference; 0 is none. A chapter of 0 is the book itself.</summary>
public readonly record struct Reference(BookName Book, int Chapter, int Verse = 0, int ToChapter = 0, int ToVerse = 0)
{
    /// <summary>A place in the book, as <see cref="Layout"/> keeps them.</summary>
    public int Place => Verse != 0 ? Chapter * 1000 + Verse : Chapter > 1 ? Chapter * 1000 : 0;

    /// <summary>"John 3:16", "John 3:36–4:2", "Psalm 23", "Psalms 22:31–23:2", "Jude 1:5", "Genesis".</summary>
    public string Named()
    {
        if (Chapter == 0) return Book.Name;
        var across = ToVerse != 0 && ToChapter != Chapter;
        return (across ? Book.Name : Book.One) + " " + Chapter
               + (Verse != 0 ? $":{Verse}" : "")
               + (across ? $"–{ToChapter}:{ToVerse}" : ToVerse != 0 && ToVerse != Verse ? $"–{ToVerse}" : "");
    }
}

/// <summary>The books, and typed references read against them.</summary>
public sealed partial class BookNames
{
    private const string Accented = "àáâäãåçèéêëēìíîïñòóôöõōùúûüýÿ";
    private const string Plain = "aaaaaaceeeeeiiiinoooooouuuuyy";

    private readonly List<BookName> _books = [];

    // The book, then what follows it: the chapter, the verse, the end of a range.
    [GeneratedRegex(@"^([1-3]?)[\s.]*(\p{L}[\p{L}\s.'’-]*?)[\s.]*([0-9].*)?$")]
    private static partial Regex ReferenceText();

    [GeneratedRegex(@"^([0-9]+)(?:\s*(?:[:.,v]|\s)\s*([0-9]+))?(?:\s*[-–—]\s*([0-9]+)(?:\s*[:.,v]\s*([0-9]+))?)?\s*$")]
    private static partial Regex Numbers();

    // What is typed before the chapter: the only part that completes.
    [GeneratedRegex(@"^\s*[1-3]?[\s.]*[\p{L}\s.'’-]*$")]
    private static partial Regex BookOnly();

    public static BookNames Load()
    {
        var names = new BookNames();
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("aov-index");
        using var reader = new StreamReader(stream!);
        while (reader.ReadLine() is { } line)
        {
            var f = line.Split('\t');
            var name = f[1];
            names._books.Add(new BookName(f[0], name, f[2] == "" ? name : f[2], f[5].Split(' ').Select(int.Parse).ToArray(),
                KeyOf(name), KeyOf(f[3]), f[4].Split('|', StringSplitOptions.RemoveEmptyEntries).Select(KeyOf).ToArray(),
                f[3], names._books.Count + 1));
        }
        return names;
    }

    public IReadOnlyList<BookName> Books => _books;

    public BookName Of(string code) => _books.First(b => b.Code == code);

    /// <summary>The books the text could be, the likeliest first; none past the book's name.</summary>
    public List<BookName> Completions(string text) => BookOnly().IsMatch(text) ? Candidates(KeyOf(text)) : [];

    /// <summary>The rest of the book's name, to show after what is typed.</summary>
    public string Ghost(string text)
    {
        if (text.Trim() == "" || Completions(text).FirstOrDefault() is not { } b) return "";
        return Fold(b.Name).StartsWith(Fold(text), StringComparison.Ordinal) ? b.Name[text.Length..] : "";
    }

    /// <summary>What the text refers to, or null. A chapter or verse past the end is the last.</summary>
    public Reference? Parse(string text)
    {
        var m = ReferenceText().Match(Fold(text.Trim()));
        if (!m.Success) return null;
        if (Candidates(KeyOf(m.Groups[1].Value + m.Groups[2].Value)).FirstOrDefault() is not { } book) return null;

        int chapter = 0, verse = 0, toChapter = 0, toVerse = 0;
        var numbered = m.Groups[3].Success;
        if (numbered)
        {
            var k = Numbers().Match(m.Groups[3].Value);
            if (!k.Success) return null;
            (chapter, verse, toChapter, toVerse) = (Num(k, 1), Num(k, 2), Num(k, 3), Num(k, 4));
        }
        if (verse == 0 && toVerse != 0) return null;                    // "jn 3-4:2"
        if (book.Verses.Length == 1 && chapter != 0 && verse == 0)      // "jude 5", "jude 3-5"
        {
            (verse, toVerse) = (chapter, toChapter);
            chapter = toChapter = 1;
        }
        else if (toVerse == 0)
        {
            (toChapter, toVerse) = verse != 0 ? (chapter, toChapter) : (0, 0);  // "jn 3-4": chapter 3
        }

        var last = book.Verses.Length;
        chapter = Math.Min(Math.Max(chapter, 1), last);
        verse = Math.Min(verse, book.Verses[chapter - 1]);
        if (toVerse != 0)
        {
            toChapter = Math.Min(Math.Max(toChapter, 1), last);
            toVerse = Math.Min(Math.Max(toVerse, 1), book.Verses[toChapter - 1]);
            if (toChapter < chapter || (toChapter == chapter && toVerse <= verse)) toChapter = toVerse = 0;
        }
        if (verse == 0) toChapter = toVerse = 0;
        return new Reference(book, numbered ? chapter : 0, verse, toChapter, toVerse);
    }

    /// <summary>A number as typed. One too long for an int is simply past the end.</summary>
    private static int Num(Match m, int group) =>
        !m.Groups[group].Success ? 0 : int.TryParse(m.Groups[group].Value, out var n) ? n : int.MaxValue;

    private List<BookName> Candidates(string key)
    {
        var found = new List<BookName>();
        if (key == "") return found;
        void Take(Func<BookName, bool> test) =>
            found.AddRange(_books.Where(b => test(b) && !found.Contains(b)).ToList());
        Take(b => b.AbbrKey == key);
        Take(b => b.NameKey == key);
        Take(b => b.AliasKeys.Contains(key));
        Take(b => Starts(b.AbbrKey, key) || Starts(b.NameKey, key));
        Take(b => b.AliasKeys.Any(a => Starts(a, key)));
        return found;
    }

    private static bool Starts(string s, string key) => s.StartsWith(key, StringComparison.Ordinal);

    /// <summary>Lower case without accents, one char for one: the app has no working Normalize.</summary>
    public static string Fold(string s)
    {
        var chars = s.ToLowerInvariant().ToCharArray();
        for (var i = 0; i < chars.Length; i++)
            if (Accented.IndexOf(chars[i]) is var at and >= 0) chars[i] = Plain[at];
        return new string(chars);
    }

    /// <summary>An x reads as ks ("ex 15" is Eksodus 15): no book's name has an x.</summary>
    private static string KeyOf(string s) =>
        new string(Fold(s).Where(char.IsLetterOrDigit).ToArray()).Replace("x", "ks");
}
