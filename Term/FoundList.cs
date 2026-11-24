namespace LeesBybel;

/// <summary>A search's verses, as a whole screen: the search on the top row, then each verse in
/// full, wrapped beside its reference, a blank row between verses. Only the rows on screen are
/// drawn; the chosen verse is kept whole on screen, its reference in the ink reversed. <c>b</c>
/// opens the books (<see cref="Picking"/>): those with results, in Bible order, each with its count
/// and a bar; the list then keeps only the books chosen there. The bottom row is always a bar of
/// the place and count, and the keys.</summary>
public sealed class FoundList
{
    public const string Prompt = "f ";       // the f line; the list's top row has "f: "
    private const string Heading = "f: ";
    private const int Most = 3;             // a bar's columns for one result, at most
    private const int First = 2;            // the top row of verses or books
    private const int Pad = 1;              // either side of a reference
    private const int Gap = 2;              // between a reference and its text
    private const int Tick = 2;             // the ✓ and a space, before a book
    private const int NameWidth = 7;        // "1 Thess"
    private const int CountWidth = 4;
    private const char Bar = '┄';

    private readonly string _typed;
    private readonly List<Hit> _all;
    private readonly bool _canonical;
    private readonly HashSet<int> _chosen;  // the books kept (by number); none: all
    private readonly List<(BookName Book, int Count)> _books;   // those with results, in Bible order
    private readonly bool _countsFull;      // the counts are of verses with every word
    private List<Hit> _hits = [];
    private int _refWidth;
    private readonly Dictionary<int, List<(int At, int Length)>> _rows = [];   // each verse's rows
    private int _room;                      // the width the rows were wrapped to
    private int _shown = 1;                 // the rows for verses, as last drawn
    private int _at;
    private int _top;

    private HashSet<int>? _picked;          // the books being chosen, while they are shown
    private int _pick;
    private int _pickTop;

    public FoundList(string typed, List<Hit> all, bool canonical, HashSet<int>? chosen = null)
    {
        (_typed, _all, _canonical, _chosen) = (typed, all, canonical, chosen ?? []);
        // Counted: verses with every typed word, or, if none has them all, every verse.
        _countsFull = all.Any(h => h.Full);
        _books = all.GroupBy(h => h.Ref.Book)
            .Select(g => (g.Key, g.Count(h => h.Full || !_countsFull)))
            .OrderBy(b => b.Key.Number).ToList();
        _chosen.IntersectWith(_books.Select(b => b.Book.Number));
        Keep();
    }

    public string Typed => _typed;
    public HashSet<int> Chosen => _chosen;
    public Hit? Current => _hits.Count == 0 ? null : _hits[_at];
    public bool Picking => _picked is not null;

    /// <summary>The verses of the chosen books, the first chosen.</summary>
    private void Keep()
    {
        _hits = _chosen.Count == 0 ? _all : _all.Where(h => _chosen.Contains(h.Ref.Book.Number)).ToList();
        _refWidth = _hits.Count == 0 ? 0 : _hits.Max(h => h.Ref.Named().Length);
        _rows.Clear();
        _room = 0;
        (_at, _top) = (0, 0);
    }

    /// <summary>Shows the books, the chosen ones ticked.</summary>
    public void Pick()
    {
        if (_books.Count == 0) return;
        _picked = [.. _chosen];
        (_pick, _pickTop) = (0, 0);
    }

    public void Key(ConsoleKeyInfo k)
    {
        if (_picked is not null)
        {
            PickKey(_picked, k);
            return;
        }
        switch (k.Key)
        {
            case ConsoleKey.UpArrow: Move(_at - 1); return;
            case ConsoleKey.DownArrow: Move(_at + 1); return;
            case ConsoleKey.PageUp: PageUp(); return;
            case ConsoleKey.PageDown or ConsoleKey.Spacebar: PageDown(); return;
            case ConsoleKey.Home: Move(0); return;
            case ConsoleKey.End: Move(_hits.Count - 1); return;
        }
    }

    private void PickKey(HashSet<int> picked, ConsoleKeyInfo k)
    {
        switch (k.Key)
        {
            case ConsoleKey.UpArrow: _pick = Math.Max(_pick - 1, 0); return;
            case ConsoleKey.DownArrow: _pick = Math.Min(_pick + 1, _books.Count - 1); return;
            case ConsoleKey.Home: _pick = 0; return;
            case ConsoleKey.End: _pick = _books.Count - 1; return;
            case ConsoleKey.Spacebar:
                var n = _books[_pick].Book.Number;
                if (!picked.Remove(n)) picked.Add(n);
                return;
            case ConsoleKey.Enter:
                _chosen.Clear();
                _chosen.UnionWith(picked);
                _picked = null;
                Keep();
                return;
            case ConsoleKey.Escape: _picked = null; return;
        }
    }

    private void Move(int to) => _at = Math.Max(Math.Min(to, _hits.Count - 1), 0);

    /// <summary>The first verse below the screen goes to the top, chosen.</summary>
    private void PageDown()
    {
        var i = _top;
        for (var used = 0; i < _hits.Count && used + Height(i) <= _shown; i++) used += Height(i) + 1;
        Move(Math.Max(i, _at + 1));
        _top = _at;
    }

    /// <summary>The verses that fit above the screen come on, the first of them chosen.</summary>
    private void PageUp()
    {
        var i = _top;
        for (var used = 0; i > 0 && used + Height(i - 1) <= _shown; i--) used += Height(i - 1) + 1;
        Move(i == _top ? i - 1 : i);
        _top = _at;
    }

    public void Draw(Screen s, int margin, int width)
    {
        s.Put(margin, 0, Heading, Attr.Dim);
        s.Put(margin + Heading.Length, 0, _typed, Attr.Ink);
        var end = margin + Heading.Length + _typed.Length;
        if (_picked is not null)
        {
            s.Put(end, 0, " — boeke", Attr.Dim);
            DrawBooks(s, margin, width, _picked);
            DrawBar(s, margin, width, $"{_picked.Count} gekies", "Spasie kies  ↵ pas toe  Esc terug");
            return;
        }
        if (_chosen.Count > 0)
            s.Put(end + 2, 0, string.Join(' ', _books.Where(b => _chosen.Contains(b.Book.Number)).Select(b => b.Book.Short)), Attr.Soft);
        var order = _canonical ? "gelyk: Bybelorde" : "gelyk: verwysings";
        s.Put(Math.Max(margin + width - order.Length, end + 2), 0, order, Attr.Dim);
        DrawVerses(s, margin, width);
        var place = _hits.Count == 0 ? "0" : $"{_at + 1}/{_hits.Count}";
        if (_chosen.Count > 0) place += $" van {_all.Count}";
        DrawBar(s, margin, width, place, "b boeke  o orde  ↵ lees  Esc terug");
    }

    /// <summary>The bottom row: what is shown on the left, the keys on the right if they fit.</summary>
    public static void DrawBar(Screen s, int margin, int width, string what, string keys, Attr look = Attr.Soft)
    {
        var y = s.H - 1;
        s.Put(0, y, new string(' ', s.W), Attr.Ink);
        s.Put(margin, y, what, look);
        if (what.Length + 2 + keys.Length <= width) s.Put(margin + width - keys.Length, y, keys, Attr.Dim);
    }

    /// <summary>The rows between the top row's and the bar.</summary>
    private static int Room(Screen s) => Math.Max(s.H - First - 1, 1);

    private void DrawVerses(Screen s, int margin, int width)
    {
        var x = margin + _refWidth + 2 * Pad + Gap;
        var room = Math.Max(margin + width - x, 2);
        if (room != _room) { _room = room; _rows.Clear(); }
        _shown = Room(s);

        _top = Math.Min(_top, _at);
        while (_top < _at && Rows(_top, _at) > _shown) _top++;

        for (int i = _top, y = First; i < _hits.Count && y < First + _shown; y += Height(i) + 1, i++)
        {
            var named = new string(' ', Pad) + _hits[i].Ref.Named().PadRight(_refWidth + Pad);
            s.Put(margin, y, named, i == _at ? Attr.Chosen : Attr.Soft);
            var text = _hits[i].Text.Replace(Search.Join, ' ');
            var rows = Wrapped(i);
            for (var r = 0; r < rows.Count && y + r < First + _shown; r++)
            {
                var (at, length) = rows[r];
                s.Put(x, y + r, text.Substring(at, length), Attr.Soft);
                foreach (var m in _hits[i].Found)
                {
                    var from = Math.Max(m.At, at);
                    var to = Math.Min(m.At + m.Length, at + length);
                    if (from < to) s.Put(x + from - at, y + r, text[from..to], m.Attr);
                }
            }
        }
    }

    /// <summary>"✓ Rom      21  ┄┄┄┄", a blank row before the New Testament. A bar is as long as
    /// its count, the largest filling the room but one result no more than <see cref="Most"/>
    /// columns; any count gets one.</summary>
    private void DrawBooks(Screen s, int margin, int width, HashSet<int> picked)
    {
        var rows = new List<int>();         // each row's book, or -1 for the blank one
        for (var i = 0; i < _books.Count; i++)
        {
            if (i > 0 && _books[i - 1].Book.Number <= 39 && _books[i].Book.Number > 39) rows.Add(-1);
            rows.Add(i);
        }
        var shown = Room(s);
        var row = rows.IndexOf(_pick);
        _pickTop = Math.Clamp(_pickTop, row - shown + 1, row);

        var barX = margin + Tick + Pad + NameWidth + 1 + CountWidth + Pad + Gap;
        var room = Math.Max(margin + width - barX, 1);
        var each = Math.Min((double)room / Math.Max(_books.Max(b => b.Count), 1), Most);
        for (var r = _pickTop; r < rows.Count && r - _pickTop < shown; r++)
        {
            if (rows[r] < 0) continue;
            var y = First + r - _pickTop;
            var (book, count) = _books[rows[r]];
            if (picked.Contains(book.Number)) s.Put(margin, y, "✓", Attr.Ink);
            var cell = new string(' ', Pad) + book.Short.PadRight(NameWidth) + " " + count.ToString().PadLeft(CountWidth) + new string(' ', Pad);
            s.Put(margin + Tick, y, cell, rows[r] == _pick ? Attr.Chosen : count > 0 ? Attr.Soft : Attr.Dim);
            if (count > 0) s.Put(barX, y, new string(Bar, Math.Max((int)Math.Round(count * each), 1)), Attr.Dim);
        }
    }

    /// <summary>The rows from the top of verse <paramref name="from"/> to the end of <paramref name="to"/>.</summary>
    private int Rows(int from, int to)
    {
        var n = 0;
        for (var i = from; i <= to; i++) n += Height(i) + (i > from ? 1 : 0);
        return n;
    }

    private int Height(int i) => Wrapped(i).Count;

    private List<(int At, int Length)> Wrapped(int i)
    {
        if (!_rows.TryGetValue(i, out var rows)) _rows[i] = rows = Wrap(_hits[i].Text, Math.Max(_room, 2));
        return rows;
    }

    /// <summary>Word wrap at spaces (not at <see cref="Search.Join"/>); a word longer than a row is cut.</summary>
    private static List<(int At, int Length)> Wrap(string text, int room)
    {
        var rows = new List<(int, int)>();
        var at = 0;
        while (true)
        {
            while (at < text.Length && text[at] == ' ') at++;
            if (at >= text.Length) break;
            var end = Math.Min(at + room, text.Length);
            if (end < text.Length && text[end] != ' ')
            {
                var space = text.LastIndexOf(' ', end - 1, end - at);
                if (space > at) end = space;
            }
            var trimmed = end;
            while (trimmed > at && text[trimmed - 1] == ' ') trimmed--;
            rows.Add((at, trimmed - at));
            at = end;
        }
        if (rows.Count == 0) rows.Add((0, 0));
        return rows;
    }
}
