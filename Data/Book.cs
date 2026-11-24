using System.Reflection;

namespace LeesBybel;

public enum BlockType { Chapter, Heading, Blank, Para }

/// <summary>Where a verse starts in a paragraph's text.</summary>
public readonly record struct VerseStart(int Offset, int Verse);

/// <summary>One block of a book; a book's file says each on a line: c a chapter, h a heading, b a blank
/// row, p a paragraph or a line of poetry.</summary>
public sealed record Block(BlockType Type, int Chapter = 0, string Kind = "", string Style = "",
    int Level = 0, string Text = "", string Letter = "", VerseStart[]? Verses = null);

/// <summary>A book of the AOV, read from the packed text in the binary.</summary>
public sealed class Book
{
    public string Pre { get; private init; } = "";
    public string Title { get; private init; } = "";
    public bool Psalms { get; private init; }
    public List<Block> Blocks { get; } = [];

    /// <summary>The book, or null if there is none by that code.</summary>
    public static Book? Load(string code)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"aov-{code}");
        if (stream is null) return null;

        using var reader = new StreamReader(stream);
        var title = reader.ReadLine()!.Split('\t');
        var book = new Book { Pre = title[1], Title = title[2], Psalms = code == "PSA" };
        var chapter = 0;
        while (reader.ReadLine() is { } line)
        {
            var f = line.Split('\t');
            switch (f[0])
            {
                case "c":
                    chapter = int.Parse(f[1]);
                    book.Blocks.Add(new Block(BlockType.Chapter, chapter));
                    break;
                case "b":
                    book.Blocks.Add(new Block(BlockType.Blank, chapter));
                    break;
                case "h":
                    book.Blocks.Add(new Block(BlockType.Heading, chapter, f[1], f[2], int.Parse(f[3]), f[4],
                        f.Length > 5 ? f[5] : ""));
                    break;
                default:
                    var (text, verses) = Marked(f[4]);
                    book.Blocks.Add(new Block(BlockType.Para, chapter, f[1], f[2], int.Parse(f[3]), text, "", verses));
                    break;
            }
        }
        return book;
    }

    /// <summary>Takes the {16} marks out of a paragraph's text.</summary>
    private static (string, VerseStart[]) Marked(string packed)
    {
        var verses = new List<VerseStart>();
        var text = new System.Text.StringBuilder(packed.Length);
        var at = 0;
        while (packed.IndexOf('{', at) is var open and >= 0)
        {
            var close = packed.IndexOf('}', open);
            text.Append(packed, at, open - at);
            verses.Add(new VerseStart(text.Length, int.Parse(packed.AsSpan(open + 1, close - open - 1))));
            at = close + 1;
        }
        text.Append(packed, at, packed.Length - at);
        return (text.ToString(), verses.ToArray());
    }
}
