using System.Globalization;
using System.Reflection;
using System.Text;

namespace LeesBybel;

/// <summary>A record of books/search.txt: a verse, or a part of one (a verse may be stored as
/// several records).</summary>
public sealed record VerseRecord(int BookId, int AovCh, int AovVs, string AovText);

/// <summary>The AOV text held in memory as one string, plus a folded copy of the same length:
/// what the search works on.</summary>
public sealed class Corpus
{
    public required VerseRecord[] Verses { get; init; }  // sorted by id
    public required string Text { get; init; }           // all verses joined by '\n'
    public required string Folded { get; init; }         // Text folded: lower case, no diacritics, ’ -> '
    public required int[] Starts { get; init; }          // start offset of each verse in Text
    public required double[] XrefIn70 { get; init; }     // 70% the percentile of In, 30% that of Out (0-100)
    public required int[][] XrefLinks { get; init; }     // the verses this verse references or is referenced by

    // Folded word -> its related forms (as spelt in the AOV) and their scores (Scores.FormScores), both directions.
    public required Dictionary<string, List<(string Form, int Score)>> Forms { get; init; }
    // Folded word -> its related words, as spelt in the AOV; one way, a row per direction.
    public required Dictionary<string, List<(string Word, int Score)>> Related { get; init; }

    public static Corpus Load()
    {
        var verses = new List<VerseRecord>();
        var in70 = new List<double>();
        var later = new List<string>();
        using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("search"))
        using (var reader = new StreamReader(stream!))
            while (reader.ReadLine() is { } line)
            {
                var f = line.Split('\t');
                verses.Add(new VerseRecord(int.Parse(f[0]), int.Parse(f[1]), int.Parse(f[2]), f[5]));
                in70.Add(double.Parse(f[3], CultureInfo.InvariantCulture));
                later.Add(f[4]);
            }
        var links = verses.Select(_ => new List<int>()).ToArray();
        for (var i = 0; i < later.Count; i++)      // a link is packed once, at the earlier verse
            foreach (var j in later[i].Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse))
            {
                links[i].Add(j);
                links[j].Add(i);
            }

        var sb = new StringBuilder(4_200_000);
        var starts = new int[verses.Count];
        for (var i = 0; i < verses.Count; i++)
        {
            starts[i] = sb.Length;
            sb.Append(verses[i].AovText).Append('\n');
        }
        var text = sb.ToString();
        return new Corpus
        {
            Verses = [.. verses],
            Text = text,
            Folded = Fold.Apply(text),
            Starts = starts,
            XrefIn70 = [.. in70],
            XrefLinks = [.. links.Select(l => l.ToArray())],
            Forms = LoadForms(),
            Related = LoadRelated(),
        };
    }

    static Dictionary<string, List<(string Form, int Score)>> LoadForms()
    {
        var forms = new Dictionary<string, List<(string, int)>>();
        foreach (var f in Rows("forms"))
        {
            if (!Scores.FormScores.TryGetValue(f[2], out int score)) continue;
            Add(f[0], f[1], score, forms);
            Add(f[1], f[0], score, forms);
        }
        return forms;
    }

    // A row works one way: a typed word also finds its related word.
    static Dictionary<string, List<(string Word, int Score)>> LoadRelated()
    {
        var related = new Dictionary<string, List<(string, int)>>();
        foreach (var f in Rows("related"))
            if (Scores.RelatedScores.TryGetValue(f[2], out int score)) Add(f[0], f[1], score, related);
        return related;
    }

    static void Add(string word, string other, int score, Dictionary<string, List<(string, int)>> to)
    {
        string key = Fold.Apply(word);
        if (!to.TryGetValue(key, out var l)) to[key] = l = [];
        l.Add((other, score));
    }

    static IEnumerable<string[]> Rows(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
        using var reader = new StreamReader(stream!);
        while (reader.ReadLine() is { } line) yield return line.Split('\t');
    }

    public string VerseText(int v) => Verses[v].AovText;

    public int VerseIndexAt(int pos)
    {
        int i = Array.BinarySearch(Starts, pos);
        return i >= 0 ? i : ~i - 1;
    }
}

/// <summary>One-to-one character folding, so folded and original positions stay aligned: its table is
/// read from books/fold.txt, since Normalize doesn't work here (InvariantGlobalization).</summary>
public static class Fold
{
    static readonly char[] Table = Build();

    static char[] Build()
    {
        var t = new char[65536];
        for (int i = 0; i < t.Length; i++) t[i] = (char)i;
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("fold");
        using var reader = new StreamReader(stream!);
        while (reader.ReadLine() is { } line)
            t[Convert.ToInt32(line[..4], 16)] = (char)Convert.ToInt32(line[5..], 16);
        return t;
    }

    public static string Apply(string s) => string.Create(s.Length, s, static (span, src) =>
    {
        for (int i = 0; i < span.Length; i++) span[i] = Table[src[i]];
    });
}
