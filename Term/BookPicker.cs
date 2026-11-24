namespace LeesBybel;

/// <summary>b: the books in a grid, Bible order left to right, the testaments apart and each group
/// in its colour; Enter shows a book's chapters, Enter there goes. Typing opens the / line, the
/// grid following the book typed. One chapter's book goes straight to it.</summary>
public sealed class BookPicker(BookNames names, BookName current, int chapter)
{
    private const string Heading = "b: ";
    private const int First = 2;            // the grid's top row
    private const int BookCell = 9;         // " 1 Thess "
    private const int ChapterCell = 5;      // " 150 "
    private const int MostAcross = 10;      // chapters to a row
    private const int Old = 39;             // the Old Testament's books

    // Each group by its last book and its colour (0 blue, 1 green, 2 pink, 3 purple, 4 orange,
    // 5 yellow): neighbours far apart in hue, and the law and the gospels,
    // the histories and Acts, the major prophets and Revelation alike.
    private static readonly (int Last, string Name, int Colour)[] Groups =
    [
        (5, "Wet", 0), (17, "Geskiedenis", 4), (22, "Poësie", 1), (27, "Groot profete", 2), (39, "Klein profete", 5),
        (43, "Evangelies", 0), (44, "Handelinge", 4), (57, "Paulus", 3), (65, "Algemene briewe", 1), (66, "Openbaring", 2),
    ];

    private readonly IReadOnlyList<BookName> _books = names.Books;
    private int _book = current.Number - 1;
    private BookName? _open;                // its chapters shown
    private int _chapter;
    private int _top;                       // the first grid row shown
    private int _width = 80;                // as last drawn

    public GoToLine? Line { get; private set; }
    public bool Closed { get; private set; }
    public Reference? Chosen { get; private set; }
    public string? Sought { get; private set; }

    private static (string Name, int Colour) Group(BookName b) =>
        Groups.Where(g => b.Number <= g.Last).Select(g => (g.Name, g.Colour)).First();

    private static Attr Shade(Attr first, int colour) => (Attr)((int)first + colour);

    private int Count => _open is null ? _books.Count : _open.Verses.Length;
    private int At => _open is null ? _book : _chapter;
    private int Width => _open is null ? BookCell : ChapterCell;
    private int Across => _open is null ? Math.Max(_width / BookCell, 1) : Math.Clamp(_width / ChapterCell, 1, MostAcross);

    /// <summary>A cell's grid row and column, and its row on screen from the grid's top (the New
    /// Testament a row lower).</summary>
    private (int Row, int Col, int Y) Cell(int i)
    {
        var across = Across;
        if (_open is not null) return (i / across, i % across, i / across);
        if (i < Old) return (i / across, i % across, i / across);
        var row = (Old + across - 1) / across + (i - Old) / across;
        return (row, (i - Old) % across, row + 1);
    }

    public void Key(ConsoleKeyInfo k)
    {
        if (Line is not null)
        {
            Typing(Line, k);
            return;
        }
        if (k.KeyChar == '/' || char.IsLetterOrDigit(k.KeyChar))
        {
            Line = new GoToLine(names);
            if (k.KeyChar != '/') Typing(Line, k);
            return;
        }
        switch (k.Key)
        {
            case ConsoleKey.LeftArrow: Move(At - 1); return;
            case ConsoleKey.RightArrow: Move(At + 1); return;
            case ConsoleKey.UpArrow: Vertical(-1); return;
            case ConsoleKey.DownArrow: Vertical(1); return;
            case ConsoleKey.Home: Move(0); return;
            case ConsoleKey.End: Move(Count - 1); return;
            case ConsoleKey.Enter:
                if (_open is null) Open(_books[_book]);
                else Chosen = new Reference(_open, _chapter + 1);
                return;
            case ConsoleKey.Escape:
                if (_open is null) Closed = true;
                else _open = null;
                return;
        }
    }

    private void Move(int to)
    {
        to = Math.Clamp(to, 0, Count - 1);
        if (_open is null) _book = to;
        else _chapter = to;
    }

    /// <summary>To the same column a row up or down, or that row's last cell if it is shorter.</summary>
    private void Vertical(int by)
    {
        var (row, col, _) = Cell(At);
        var cells = Enumerable.Range(0, Count).Where(i => Cell(i).Row == row + by).ToList();
        if (cells.Count == 0) return;
        Move(cells.FirstOrDefault(i => Cell(i).Col == col, cells[^1]));
    }

    private void Open(BookName book)
    {
        if (book.Verses.Length == 1)
        {
            Chosen = new Reference(book, 1);
            return;
        }
        (_open, _book, _top) = (book, book.Number - 1, 0);
        _chapter = book == current ? Math.Clamp(chapter, 1, book.Verses.Length) - 1 : 0;
    }

    /// <summary>The / line: a reference goes, a book's name opens its chapters, other words search.
    /// The grid follows the book typed.</summary>
    private void Typing(GoToLine line, ConsoleKeyInfo k)
    {
        switch (k.Key)
        {
            case ConsoleKey.Escape: Line = null; return;
            case ConsoleKey when line.Closes(k): Line = null; return;
            case ConsoleKey.Enter:
                if (line.Target is { } target) Chosen = target;
                else if (Named(line) is { } book)
                {
                    Line = null;
                    Open(book);
                }
                else if (line.Searches) Sought = line.Text;
                return;
        }
        line.Key(k);
        if (Named(line) is { } b) (_open, _book) = (null, b.Number - 1);
    }

    private BookName? Named(GoToLine line) => line.Target?.Book ?? names.Completions(line.Text).FirstOrDefault();

    /// <summary>What Enter on the line will do, for its right end.</summary>
    public string? Aim(GoToLine line) =>
        line.Target is { } t ? "→ " + t.Named()
        : Named(line) is { } b ? (b.Verses.Length == 1 ? "→ " + b.Name : "↵ hoofstukke")
        : line.Searches ? "↵ Soek" : null;

    public void Draw(Screen s, int margin, int width)
    {
        _width = width;
        var book = _open ?? _books[_book];
        s.Put(margin, 0, Heading, Attr.Dim);
        s.Put(margin + Heading.Length, 0, book.Name, Attr.Ink);
        if (_open is not null) s.Put(margin + Heading.Length + book.Name.Length, 0, " — hoofstukke", Attr.Dim);

        var room = Math.Max(s.H - First - 1, 1);
        var y = Cell(At).Y;
        _top = Math.Clamp(_top, y - room + 1, y);
        for (var i = 0; i < Count; i++)
        {
            var (_, col, cy) = Cell(i);
            var row = First + cy - _top;
            if (row < First || row >= First + room) continue;
            var (text, colour) = _open is null
                ? (" " + _books[i].Short.PadRight(BookCell - 2) + " ", Group(_books[i]).Colour)
                : (" " + (i + 1).ToString().PadLeft(ChapterCell - 2) + " ", Group(_open).Colour);
            s.Put(margin + col * Width, row, text, Shade(i == At ? Attr.Mark0 : Attr.Part0, colour));
        }

        if (_open is null)
        {
            var (name, colour) = Group(book);
            var keys = book.Verses.Length == 1 ? "↵ lees  tik: soek  Esc terug" : "↵ hoofstukke  tik: soek  Esc terug";
            FoundList.DrawBar(s, margin, width, name, keys, Shade(Attr.Part0, colour));
        }
        else FoundList.DrawBar(s, margin, width, $"{Count} hoofstukke", "↵ lees  tik: soek  Esc boeke");
    }
}
