namespace LeesBybel;

/// <summary>The three toggles: each off, or shown dim. Subsection headings are left out.</summary>
public readonly record struct Show(bool Verses, bool Chapters, bool Headings);

/// <summary>Part of a row in another look: a number.</summary>
public readonly record struct Mark(int At, int Length, Attr Attr);

/// <summary>One row of the text, at column <see cref="X"/> within the measure.</summary>
public readonly record struct Line(string Text, int X, Attr Attr, Mark[]? Marks = null);

/// <summary>
/// A book laid out for a width and a set of toggles. It also maps rows to verses, both
/// ways. A verse key is chapter * 1000 + verse; a chapter's own key has verse 0; the top of the
/// book is 0.
/// </summary>
public sealed class Layout
{
    private const int Indent = 3;            // a prose paragraph's first line
    private const int RuleWidth = 6;         // the rule between psalms
    private const int Subsection = 3;        // the heading level left out
    private const char Join = '\uE000';      // holds a verse number to its verse: a space that won't wrap
    private static readonly Line Blank = new("", 0, Attr.Ink);

    private readonly List<(int Row, int Key)> _verses = [];
    private readonly Dictionary<int, int> _rows = [];
    private readonly Dictionary<int, int> _headings = [];   // a verse's heading's first row, if one leads into it
    private readonly List<Line> _lines = [];

    private const int Hang = 2;              // poetry's rows after the first

    private readonly int _width;
    private readonly Show _show;
    private int _gap;                         // blank rows owed before the next row
    private int _opening;                     // a chapter waiting for its first row
    private bool _markerOwed;                 // a psalm's number or rule, the same
    private string _prior = "";               // the block just before: its kind, "" if none
    private int _lastVerse;                   // the last verse seen in this chapter
    private int? _headed;                     // the first row of the headings since the last paragraph

    public IReadOnlyList<Line> Lines => _lines;

    public Layout(Book book, int width, Show show)
    {
        _width = width;
        _show = show;
        Title(book);
        foreach (var b in book.Blocks)
        {
            switch (b.Type)
            {
                case BlockType.Chapter:
                    _opening = b.Chapter;
                    _lastVerse = 0;
                    _markerOwed = book.Psalms;
                    break;
                case BlockType.Blank:
                    Marker();
                    Gap(1);
                    _prior = "";
                    break;
                case BlockType.Heading:
                    Heading(b);
                    break;
                case BlockType.Para:
                    Marker();
                    Para(b);
                    break;
            }
        }
    }

    /// <summary>The place for a row: the last verse to begin at or above it, the first of
    /// those beginning on one row (so a place put there is found again), or the top.</summary>
    public int PlaceAt(int row)
    {
        var i = LastAt(row);
        if (i < 0) return 0;
        return _verses[FirstOnRow(i)].Key;
    }

    /// <summary>The verses on <paramref name="rows"/> rows from <paramref name="top"/>, for the
    /// status bar: the one at the top, even if it began above, to the last that starts on them.
    /// The first is the place's verse, so a jump to a verse names that verse; with a heading at
    /// the top, the verse it leads into.</summary>
    public (int From, int To) Span(int top, int rows)
    {
        var from = Math.Max(LastAt(top), 0);
        var next = LastAt(top) + 1;
        if (next < _verses.Count && _headings.TryGetValue(_verses[next].Key, out var head) && head <= top)
            from = next;
        return (_verses[FirstOnRow(from)].Key, _verses[Math.Max(LastAt(top + rows - 1), 0)].Key);
    }

    private int FirstOnRow(int i)
    {
        while (i > 0 && _verses[i - 1].Row == _verses[i].Row) i--;
        return i;
    }

    /// <summary>The index of the last verse to begin at or above a row, -1 if none.</summary>
    private int LastAt(int row)
    {
        int lo = 0, hi = _verses.Count;
        while (lo < hi)
        {
            var mid = (lo + hi) / 2;
            if (_verses[mid].Row <= row) lo = mid + 1;
            else hi = mid;
        }
        return lo - 1;
    }

    /// <summary>The row for a place. A verse the book lacks goes to its chapter.</summary>
    public int RowOf(int place) =>
        place == 0 ? 0
        : _rows.TryGetValue(place, out var row) ? row
        : _rows.GetValueOrDefault(place / 1000 * 1000);

    /// <summary>The row for a place gone to: its heading's, if one leads into it, so the heading shows.</summary>
    public int HeadedRowOf(int place) => _headings.TryGetValue(place, out var row) ? row : RowOf(place);

    private void Title(Book book)
    {
        _lines.Add(Blank);
        if (book.Pre != "") Centred(book.Pre, Attr.Soft);
        Centred(book.Title, Attr.Head);
        _gap = 2;
    }

    private void Heading(Block b)
    {
        var shown = _show.Headings && b.Level != Subsection;
        switch (b.Kind)
        {
            case "acrostic":        // the poem's own form, so always shown; the name, not the letter
                Marker();
                Gap(1);
                Head();
                Add(new Line(b.Text, 0, Attr.Head));
                break;
            case "psalter_book_range":
                if (!shown) return; // no gap of its own
                Head();
                Rows(b.Text, Attr.Head, centred: true);
                _gap = 1;
                return;
            case "psalter_book" or "division":
                Gap(2);
                if (shown) Head();
                if (shown) Rows(b.Text, Attr.Head, centred: true);
                break;
            default:
                Marker();
                var centred = _prior == "rule";
                Gap(1);
                if (shown) Head();
                if (shown) Rows(b.Text, Attr.Head, centred);
                break;
        }
        _prior = "heading";
    }

    /// <summary>A heading shown here: the first of them leads into the next paragraph's first verse.</summary>
    private void Head() => _headed ??= _lines.Count + _gap;

    /// <summary>A psalm's number or rule comes after a "BOOK I", before the psalm itself.
    /// With numbers off, Psalm 1 has neither.</summary>
    private void Marker()
    {
        if (!_markerOwed) return;
        _markerOwed = false;
        var shown = _show.Chapters;
        if (shown || _opening > 1)
        {
            Gap(2);
            if (shown) Centred(Numeral(_opening), Attr.Number);
            else Centred(new string('─', RuleWidth), Attr.Faint);
            _rows[_opening * 1000] = _lines.Count - 1;
            _opening = 0;
            _gap = 1;
        }
        _prior = "rule";
    }

    /// <summary>A chapter's number, in brackets so that it stands out from the verse numbers.</summary>
    private static string Numeral(int chapter) => $"[{chapter}]";

    private void Rows(string text, Attr attr, bool centred)
    {
        foreach (var (at, length) in Wrap(text, 0, 0))
        {
            var row = text.Substring(at, length);
            if (centred) Centred(row, attr);
            else Add(new Line(row, 0, attr));
        }
    }

    /// <summary>A paragraph's text with its verse numbers in, if they show, and where each
    /// verse starts in that text. The chapter numeral, if shown, stands for verse 1.</summary>
    private (string Text, int[] Starts, List<(int At, int Length)> Numbers) Numbered(Block b)
    {
        var verses = b.Verses!;
        var starts = new int[verses.Length];
        var numbers = new List<(int, int)>();
        if (!_show.Verses)
        {
            for (var i = 0; i < verses.Length; i++) starts[i] = verses[i].Offset;
            return (b.Text, starts, numbers);
        }

        var text = new System.Text.StringBuilder(b.Text.Length + verses.Length * 4);
        var from = 0;
        for (var i = 0; i < verses.Length; i++)
        {
            var v = verses[i];
            text.Append(b.Text, from, v.Offset - from);
            from = v.Offset;
            if (v.Verse != 1 || !_show.Chapters)
            {
                if (text.Length > 0 && text[^1] != ' ') text.Append(' ');   // after a dash
                var digits = v.Verse.ToString();
                numbers.Add((text.Length, digits.Length));
                starts[i] = text.Length;
                text.Append(digits).Append(Join);
            }
            else starts[i] = text.Length;
        }
        text.Append(b.Text, from, b.Text.Length - from);
        return (text.ToString(), starts, numbers);
    }

    private void Para(Block b)
    {
        var verses = b.Verses!;
        var opens = _opening != 0;
        var chapters = _show.Chapters;
        var prose = b.Kind == "prose";
        var flowing = b.Kind is "poetry" or "list";

        // Prose and verse apart; a chapter with nothing before it to mark it (the AOV's).
        if ((_prior == "prose" && flowing) || (prose && _prior is "poetry" or "list")) Gap(1);
        if (opens && !chapters && _prior is not ("" or "heading" or "rule")) Gap(1);

        var (first, rest, attr) = (b.Kind, b.Style) switch
        {
            ("prose", _) => (_prior == "prose" && !opens && _gap == 0 ? Indent : 0, 0, Attr.Soft),
            ("poetry", "q2") => (Hang / 2, Hang, Attr.Soft),
            ("poetry", _) => (0, Hang, Attr.Soft),
            ("list", "li2") => (4, 6, Attr.Soft),
            ("list", _) => (2, 4, Attr.Soft),
            ("superscription", _) => (0, 0, Attr.Soft),
            _ => (0, 0, Attr.Soft),
        };

        // The chapter's number starts the first row, at the left edge.
        var numeral = opens && chapters ? Numeral(b.Chapter) : null;
        if (numeral is not null) first = Math.Max(first, numeral.Length + 1);

        var (text, starts, numbers) = Numbered(b);
        var spans = Wrap(text, first, rest);
        var start = _lines.Count + _gap;
        if (opens)
        {
            _rows[_opening * 1000] = start;
            _opening = 0;
        }
        for (var i = 0; i < spans.Count; i++)
        {
            var (at, length) = spans[i];
            var row = text.Substring(at, length).Replace(Join, ' ');
            var x = i == 0 ? first : rest;
            if (b.Kind == "refrain") x = Math.Max(_width - length - 1, 0);
            else if (b.Kind == "inscription") x = Math.Max((_width - length) / 2, 0);

            var marks = new List<Mark>();
            if (i == 0 && numeral is not null)
            {
                row = numeral.PadRight(first) + row;
                marks.Add(new Mark(0, numeral.Length, Attr.Number));
            }
            var shift = row.Length - length;
            foreach (var n in numbers)
                if (n.At >= at && n.At < at + length)
                    marks.Add(new Mark(n.At - at + shift, n.Length, Attr.Number));
            if (shift > 0) x = 0;
            Add(new Line(row, x, attr, marks.Count > 0 ? marks.ToArray() : null));
        }

        for (var i = 0; i < verses.Length; i++)
        {
            var v = verses[i];
            var row = start + RowIn(spans, starts[i]);
            var key = b.Chapter * 1000 + v.Verse;
            if (v.Verse == 1) row = _rows.GetValueOrDefault(b.Chapter * 1000, row);
            _verses.Add((row, key));
            _rows.TryAdd(key, row);

            // Verses left out of a chapter can still be gone to.
            for (var gone = _lastVerse + 1; _lastVerse > 0 && gone < v.Verse; gone++)
                _rows.TryAdd(b.Chapter * 1000 + gone, row);
            _lastVerse = v.Verse;
        }
        // The headings above lead into the verse that starts the paragraph (and its chapter, for a verse 1).
        if (_headed is { } head && verses.Length > 0 && RowIn(spans, starts[0]) == 0)
        {
            var key = b.Chapter * 1000 + verses[0].Verse;
            _headings.TryAdd(key, head);
            if (verses[0].Verse == 1) _headings.TryAdd(b.Chapter * 1000, head);
        }
        _headed = null;
        _prior = b.Kind;
    }

    private static int RowIn(List<(int At, int Length)> spans, int offset)
    {
        for (var i = 0; i < spans.Count; i++)
            if (spans[i].At + spans[i].Length > offset) return i;
        return spans.Count - 1;
    }

    /// <summary>Word wrap: the first row at <paramref name="first"/>, the rest at <paramref name="rest"/>.</summary>
    private List<(int At, int Length)> Wrap(string text, int first, int rest)
    {
        var spans = new List<(int, int)>();
        var at = 0;
        while (true)
        {
            while (at < text.Length && text[at] == ' ') at++;
            if (at >= text.Length) break;

            var room = Math.Max(_width - (spans.Count == 0 ? first : rest), 1);
            var end = Math.Min(at + room, text.Length);
            if (end < text.Length && text[end] != ' ')
            {
                var space = text.LastIndexOf(' ', end - 1, end - at);
                if (space > at) end = space;
            }
            var trimmed = end;
            while (trimmed > at && text[trimmed - 1] == ' ') trimmed--;
            spans.Add((at, trimmed - at));
            at = end;
        }
        if (spans.Count == 0) spans.Add((0, 0));
        return spans;
    }

    private void Centred(string text, Attr attr) =>
        Add(new Line(text, Math.Max((_width - text.Length) / 2, 0), attr));

    private void Gap(int rows) => _gap = Math.Max(_gap, rows);

    private void Add(Line line)
    {
        for (; _gap > 0; _gap--) _lines.Add(Blank);
        _lines.Add(line);
    }
}
