using System.Runtime.InteropServices;

namespace LeesBybel;

/// <summary>The reader: a book, scrolled by rows, keeping its place across a change.</summary>
public sealed class App(Screen s, string code, Book book)
{
    private const int Measure = 66;         // 60–70 characters read best

    private string _code = code;
    private Book _book = book;
    private readonly BookNames _names = BookNames.Load();
    private Show _show = new(Verses: true, Chapters: true, Headings: true);     // all on
    private Layout? _layout;
    private GoToLine? _goTo;                // the reference being typed, while the line is open
    private Field? _typing;                 // the search being typed, while its line is open
    private FoundList? _found;              // the last search
    private bool _canonical;                // equal scores in Bible order, not by cross-references
    private bool _listing;                  // its list covers the text
    private BookPicker? _picker;            // b: it covers the text
    private bool _status = true;            // on at the start
    private bool _help;                     // the keys cover the text
    private int _margin;
    private int _width;
    private int _top;
    private int? _held = 0;                 // the place to keep through a change, until a scroll
    private bool _gone;                     // the place was gone to: its heading shows too
    private volatile bool _running = true;

    /// <summary>Rows of text: the status bar takes one. The reference line only covers one.</summary>
    private int Rows => Math.Max(s.H - (_status ? 1 : 0), 1);

    public void Stop() => _running = false;

    public void Run()
    {
        // Ending by a signal runs the same way out as q, so the terminal is restored.
        using var term = PosixSignalRegistration.Create(PosixSignal.SIGTERM, Signalled);
        using var interrupt = PosixSignalRegistration.Create(PosixSignal.SIGINT, Signalled);

        Draw();
        while (_running)
        {
            if (!Console.KeyAvailable)
            {
                if (s.SizeChanged()) Draw();
                Thread.Sleep(20);
                continue;
            }
            while (_running && Console.KeyAvailable) Handle(Console.ReadKey(intercept: true));
            if (_running) Draw();
        }
    }

    private void Signalled(PosixSignalContext context)
    {
        context.Cancel = true;
        Stop();
    }

    private void Handle(ConsoleKeyInfo k)
    {
        if (_help)
        {
            _help = false;
            if (k.Key == ConsoleKey.C && k.Modifiers.HasFlag(ConsoleModifiers.Control)) Stop();
            return;
        }
        if (_goTo is not null)
        {
            Typing(_goTo, k);
            return;
        }
        if (_typing is not null)
        {
            Seeking(_typing, k);
            return;
        }
        if (_picker is not null)
        {
            Picking(_picker, k);
            return;
        }
        if (_listing)
        {
            Listing(_found!, k);
            return;
        }
        if (k.KeyChar == '/')
        {
            _goTo = new GoToLine(_names);
            return;
        }
        if (k.KeyChar == '?')
        {
            _help = true;
            return;
        }

        var page = Math.Max(Rows - 2, 1);   // a page keeps two rows of the last one
        switch (k.Key)
        {
            case ConsoleKey.UpArrow: Scroll(-1); return;
            case ConsoleKey.DownArrow: Scroll(1); return;
            case ConsoleKey.PageUp: Scroll(-page); return;
            case ConsoleKey.PageDown or ConsoleKey.Spacebar: Scroll(page); return;
            case ConsoleKey.Home: Scroll(-_top); return;
            case ConsoleKey.End: Scroll(int.MaxValue / 2); return;
            case ConsoleKey.Q: Stop(); return;
            case ConsoleKey.C when k.Modifiers.HasFlag(ConsoleModifiers.Control): Stop(); return;
            case ConsoleKey.V: Change(_show with { Verses = !_show.Verses }); return;
            case ConsoleKey.C: Change(_show with { Chapters = !_show.Chapters }); return;
            case ConsoleKey.H: Change(_show with { Headings = !_show.Headings }); return;
            case ConsoleKey.S: _status = !_status; return;
            case ConsoleKey.K: s.Colour = (Colour)(((int)s.Colour + 1) % 3); return;
            case ConsoleKey.B:
                _picker = new BookPicker(_names, _names.Of(_code), _layout!.Span(_top, Rows).From / 1000);   // as the status bar
                return;
            case ConsoleKey.F:
                if (_found is null) _typing = new();
                else _listing = true;
                return;
        }
    }

    private void Typing(GoToLine line, ConsoleKeyInfo k)
    {
        switch (k.Key)
        {
            case ConsoleKey.Escape: _goTo = null; return;
            case ConsoleKey when line.Closes(k): _goTo = null; return;
            case ConsoleKey.C when k.Modifiers.HasFlag(ConsoleModifiers.Control): Stop(); return;
            case ConsoleKey.Enter:
                if (line.Searches)
                {
                    _goTo = null;
                    Find(line.Text);
                    return;
                }
                if (line.Target is not { } target) return;     // not found: the line stays open
                _goTo = null;
                _listing = false;
                Jump(target);
                return;
            default: line.Key(k); return;
        }
    }

    private void Seeking(Field line, ConsoleKeyInfo k)
    {
        switch (k.Key)
        {
            case ConsoleKey.Escape: _typing = null; return;
            case ConsoleKey.C when k.Modifiers.HasFlag(ConsoleModifiers.Control): Stop(); return;
            case ConsoleKey.Enter:
                if (line.Text.Trim() == "") return;
                _typing = null;
                Find(line.Text);
                return;
            default: line.Key(k); return;
        }
    }

    /// <summary>A new search: no books chosen.</summary>
    private void Find(string text)
    {
        _found = new FoundList(text, Search.Run(_names, text, _canonical), _canonical);
        _listing = true;
    }

    private void Picking(BookPicker picker, ConsoleKeyInfo k)
    {
        if (k.Key == ConsoleKey.C && k.Modifiers.HasFlag(ConsoleModifiers.Control))
        {
            Stop();
            return;
        }
        picker.Key(k);
        if (picker.Closed) _picker = null;
        else if (picker.Chosen is { } r)
        {
            _picker = null;
            Jump(r);
        }
        else if (picker.Sought is { } text)
        {
            _picker = null;
            Find(text);
        }
    }

    private void Listing(FoundList list, ConsoleKeyInfo k)
    {
        if (k.Key == ConsoleKey.C && k.Modifiers.HasFlag(ConsoleModifiers.Control))
        {
            Stop();
            return;
        }
        if (list.Picking)
        {
            list.Key(k);
            return;
        }
        if (k.KeyChar == '/')
        {
            _goTo = new GoToLine(_names);
            return;
        }
        switch (k.Key)
        {
            case ConsoleKey.Escape: _listing = false; return;
            case ConsoleKey.F: _typing = new(); return;
            case ConsoleKey.B: list.Pick(); return;
            case ConsoleKey.O:
                // The search again: which pair keeps a verse depends on the order. The books stay.
                _canonical = !_canonical;
                _found = new FoundList(list.Typed, Search.Run(_names, list.Typed, _canonical), _canonical, list.Chosen);
                return;
            case ConsoleKey.Enter:
                if (list.Current is not { } hit) return;
                _listing = false;
                Jump(hit.Ref);
                return;
            default: list.Key(k); return;
        }
    }

    /// <summary>The place goes to the top row, as far as the end of the book allows.</summary>
    private void Jump(Reference r)
    {
        if (r.Book.Code != _code)
        {
            _code = r.Book.Code;
            _book = Book.Load(_code)!;
            _layout = null;
        }
        _held = r.Place;
        _gone = true;
    }

    private void Scroll(int by)
    {
        var top = Math.Clamp(_top + by, 0, Bottom());
        if (top == _top) return;
        _top = top;
        _held = null;
        _gone = false;
    }

    private void Change(Show show)
    {
        Hold();
        _show = show;
        _layout = null;
    }

    private void Hold() => _held ??= _layout?.PlaceAt(_top);

    private int Bottom() => Math.Max(_layout!.Lines.Count - Rows, 0);

    private void Draw()
    {
        var resized = s.SizeChanged();
        if (resized) Hold();
        s.Begin();

        if (_layout is null || resized)
        {
            _width = Math.Max(Math.Min(Measure, s.W - 4), 10);
            _margin = Math.Max((s.W - _width) / 2, 0);
            _layout = new Layout(_book, _width, _show);
        }
        if (_held is { } place) _top = _gone ? _layout.HeadedRowOf(place) : _layout.RowOf(place);
        _top = Math.Clamp(_top, 0, Bottom());

        s.Cursor = null;
        if (_help)
        {
            Help.Draw(s);
            s.Flush();
            return;
        }

        if (_picker is not null)
        {
            _picker.Draw(s, _margin, _width);
            if (_picker.Line is { } line) DrawGoTo(line, _picker.Aim(line));
        }
        else if (_listing) _found!.Draw(s, _margin, _width);
        else DrawText();
        if (_goTo is not null) DrawGoTo(_goTo, Aim(_goTo));
        if (_typing is not null) DrawLine(FoundList.Prompt, _typing);
        s.Flush();
    }

    private void DrawText()
    {
        var lines = _layout!.Lines;
        for (var y = 0; y < Rows && _top + y < lines.Count; y++)
        {
            var line = lines[_top + y];
            s.Put(_margin + line.X, y, line.Text, line.Attr);
            foreach (var m in line.Marks ?? [])
                s.Put(_margin + line.X + m.At, y, line.Text.Substring(m.At, m.Length), m.Attr);
        }
        if (_status) DrawStatus();
    }

    private void DrawStatus()
    {
        var (from, to) = _layout!.Span(_top, Rows);
        var text = new Reference(_names.Of(_code), from / 1000, from % 1000, to / 1000, to % 1000).Named();
        var x = Math.Max((s.W - text.Length) / 2, 0);
        s.Put(x, s.H - 1, text, Attr.Status);
        const string help = "? hulp";
        var at = _margin + _width - help.Length;
        if (at >= x + text.Length + 2) s.Put(at, s.H - 1, help, Attr.Help);
    }

    /// <summary>The bottom row: "/ jn 3:16", then at the column's right edge what it will open, or
    /// that Enter searches.</summary>
    private void DrawGoTo(GoToLine line, string? to)
    {
        var end = DrawLine("/ ", line.Field);
        var y = s.H - 1;
        s.Put(end, y, line.Ghost, Attr.Dim);
        if (to is not null)
        {
            var at = _margin + _width - to.Length;
            if (at > end) s.Put(at, y, to, Attr.Soft);
        }
    }

    private static string? Aim(GoToLine line) =>
        line.Searches ? "↵ Soek" : line.Target is { } target ? "→ " + target.Named() : null;

    /// <summary>A line being typed, on the bottom row. Too long, it shows the part around the
    /// cursor. Returns the column after the text shown.</summary>
    private int DrawLine(string prompt, Field line)
    {
        var y = s.H - 1;
        var room = Math.Max(_width - prompt.Length - 1, 1);
        var from = Math.Max(line.At - room + 1, 0);
        var text = line.Text[from..Math.Min(from + room, line.Text.Length)];
        var x = _margin + prompt.Length;

        s.Put(0, y, new string(' ', s.W), Attr.Ink);
        s.Put(_margin, y, prompt, Attr.Dim);
        s.Put(x, y, text, Attr.Ink);
        s.Cursor = (x + line.At - from, y);
        return x + text.Length;
    }
}
