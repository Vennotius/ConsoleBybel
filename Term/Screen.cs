using System.Runtime.InteropServices;
using System.Text;

namespace LeesBybel;

/// <summary>How text looks: four shades of one colour on black. The bare console has three:
/// its Dim and Faint are the same, where a terminal with more shows Faint darker. Then a search's
/// marks, by the word's colour: Mark0–5 and MarkGrey behind a whole word, Part0–5 and PartGrey on
/// the letters of one inside a longer word or with other accents.</summary>
public enum Attr : byte
{
    Ink, Soft, Dim, Faint,
    Mark0, Mark1, Mark2, Mark3, Mark4, Mark5, MarkGrey,
    Part0, Part1, Part2, Part3, Part4, Part5, PartGrey,
    Chosen,                                 // the ink reversed: the chosen verse or book
    Head, Number,                           // titles and headings, chapter and verse numbers: in
                                            // white orange and blue, in one colour the ink
    Status, Help,                           // the status bar darker pink, its "? hulp" darker green,
                                            // in every colour
}

/// <summary>The colour of all the text, cycled with k.</summary>
public enum Colour : byte { White, Orange, Green }

/// <summary>
/// A grid of cells written to the terminal with plain ANSI: nothing but cursor moves and SGR.
/// Each frame is drawn whole, then only the rows that changed are sent.
/// </summary>
public sealed class Screen
{
    private readonly record struct Cell(char C, Attr A);

    // The shades of each colour but white, Ink first, as 0xRRGGBB.
    private static readonly int[] Orange = [0xE89C16, 0xB07612, 0x784E0A, 0x4E3308];
    private static readonly int[] Green = [0x46E074, 0x34A858, 0x247C40, 0x18522A];

    // The marks' colours: the dark ones behind a word, the last the grey; the light ones on the letters.
    private static readonly int[] Dark = [0x105B88, 0x2F6A35, 0x7C2F50, 0x56407E, 0x5F3C21, 0x695E17, 0x3F3F3B];
    private static readonly int[] Light = [0xB0D2FF, 0xBBE2C0, 0xF7C0D6, 0xD4C0F2, 0xF4CCAA, 0xEEE4A6];

    // The status bar's pink and green: halfway between the light colour and the dark.
    private const int StatusPink = 0xB97793, HelpGreen = 0x75A67A;

    private static readonly string[] Greys = ["97", "37", "90", "38;5;239"];
    private static readonly string[][] Sgr = [Shades(Greys, coloured: true), Shades(Rgb(Orange)), Shades(Rgb(Green))];

    // The bare console reduces 38;2 to 16 colours, so its greys stay and their palette entries change.
    // Marks: backgrounds 1–6 and foregrounds 9–14 take the marks' colours; grey is black on 7.
    private static readonly bool Bare = Environment.GetEnvironmentVariable("TERM") == "linux";
    private static readonly int[] GreySlots = [15, 7, 8];
    private static readonly string[][] BareSgr = [BareShades(coloured: true), BareShades(false), BareShades(false)];

    private static string[] BareShades(bool coloured) =>
    [
        .. Greys.Select(f => $"\x1b[0;{f};40m"),
        .. Enumerable.Range(1, 6).Select(n => $"\x1b[0;97;4{n}m"), "\x1b[0;30;47m",
        .. Enumerable.Range(1, 6).Select(n => $"\x1b[0;9{n};40m"), "\x1b[0;97;40m",
        "\x1b[0;97;40;7m",
        coloured ? "\x1b[0;95;40m" : "\x1b[0;97;40m",     // slot 13: orange
        coloured ? "\x1b[0;91;40m" : "\x1b[0;97;40m",     // slot 9: blue
        "\x1b[0;33;40m",                                   // slot 3: the marks' dark pink (no slot is free)
        "\x1b[0;32;40m",                                   // slot 2: the marks' dark green
    ];

    private readonly Stream _out = Console.OpenStandardOutput();
    private Cell[] _cells = [];
    private string?[] _shown = [];
    private uint _windowsMode;
    private Colour _colour = Colour.White;

    public int W { get; private set; }
    public int H { get; private set; }

    public Colour Colour
    {
        get => _colour;
        set
        {
            _colour = value;
            Array.Clear(_shown);    // the same SGR on the bare console, which may not repaint by itself
            if (Bare) Send(Palette(value));
        }
    }

    /// <summary>Where to show the cursor after a frame. Null hides it.</summary>
    public (int X, int Y)? Cursor { get; set; }

    public void Enter()
    {
        if (OperatingSystem.IsWindows()) _windowsMode = WindowsConsole.Enter();
        Send("\x1b[?1049h\x1b[?25l\x1b[?7l\x1b[0;40m\x1b[2J");
        if (Bare) Send(Palette(_colour));       // the marks' colours
    }

    /// <summary>Clears before leaving: the bare console has no alternate screen to leave.</summary>
    public void Leave()
    {
        if (Bare) Send("\x1b]R");   // the palette goes back
        Send("\x1b[0m\x1b[2J\x1b[H\x1b[?7h\x1b[?25h\x1b[?1049l");
        if (OperatingSystem.IsWindows()) WindowsConsole.Leave(_windowsMode);
    }

    public bool SizeChanged() => Math.Max(Console.WindowWidth, 1) != W || Math.Max(Console.WindowHeight, 1) != H;

    /// <summary>Starts a frame: picks up any resize and blanks the grid.</summary>
    public void Begin()
    {
        if (SizeChanged())
        {
            W = Math.Max(Console.WindowWidth, 1);
            H = Math.Max(Console.WindowHeight, 1);
            _cells = new Cell[W * H];
            _shown = new string?[H];
            Send("\x1b[0;40m\x1b[2J");
        }
        Array.Fill(_cells, new Cell(' ', Attr.Ink));
    }

    public void Put(int x, int y, string text, Attr a)
    {
        if (y < 0 || y >= H) return;
        foreach (var ch in text)
        {
            if (x >= 0 && x < W) _cells[y * W + x] = new Cell(ch, a);
            x++;
        }
    }

    public void Flush()
    {
        var frame = new StringBuilder();
        var row = new StringBuilder();
        var sgr = (Bare ? BareSgr : Sgr)[(int)_colour];
        for (var y = 0; y < H; y++)
        {
            row.Clear();
            Attr? last = null;
            for (var x = 0; x < W; x++)
            {
                var c = _cells[y * W + x];
                if (c.A != last)
                {
                    row.Append(sgr[(int)c.A]);
                    last = c.A;
                }
                row.Append(c.C);
            }

            var line = row.ToString();
            if (line == _shown[y]) continue;
            _shown[y] = line;
            frame.Append("\x1b[").Append(y + 1).Append(";1H").Append(line);
        }
        if (Cursor is { } at) frame.Append("\x1b[").Append(at.Y + 1).Append(';').Append(at.X + 1).Append("H\x1b[?25h");
        else frame.Append("\x1b[?25l");
        Send(frame.ToString());
    }

    private static string[] Rgb(int[] colours) => colours.Select(c => $"38;2;{c >> 16};{c >> 8 & 255};{c & 255}").ToArray();

    /// <summary>The SGR of each Attr, from a colour's four shades: a mark is the ink on a dark
    /// colour, a part a light colour on black (its grey the ink). Coloured, headings are orange
    /// and numbers blue; otherwise both the ink.</summary>
    private static string[] Shades(string[] fg, bool coloured = false) =>
    [
        .. fg.Select(f => $"\x1b[0;{f};40m"),
        .. Dark.Select(c => $"\x1b[0;{fg[0]};48;2;{c >> 16};{c >> 8 & 255};{c & 255}m"),
        .. Rgb(Light).Select(f => $"\x1b[0;{f};40m"),
        $"\x1b[0;{fg[0]};40m",
        $"\x1b[0;{fg[0]};40;7m",
        $"\x1b[0;{(coloured ? Rgb(Light)[4] : fg[0])};40m",
        $"\x1b[0;{(coloured ? Rgb(Light)[0] : fg[0])};40m",
        $"\x1b[0;{Rgb([StatusPink])[0]};40m",
        $"\x1b[0;{Rgb([HelpGreen])[0]};40m",
    ];

    /// <summary>ESC ] P sets a palette entry, ESC ] R resets them all.</summary>
    private static string Palette(Colour colour)
    {
        static string Slot(int slot, int c) => $"\x1b]P{slot:X}{c:x6}";
        var shades = colour == Colour.Orange ? Orange : colour == Colour.Green ? Green : [];
        return "\x1b]R" + string.Concat(shades.Take(GreySlots.Length).Select((c, i) => Slot(GreySlots[i], c)))
            + string.Concat(Dark[..6].Select((c, i) => Slot(1 + i, c)))
            + string.Concat(Light.Select((c, i) => Slot(9 + i, c)));
    }

    private void Send(string s)
    {
        _out.Write(Encoding.UTF8.GetBytes(s));
        _out.Flush();
    }
}

/// <summary>UTF-8 output and ANSI codes on the Windows console.</summary>
internal static class WindowsConsole
{
    private const int StdOutput = -11;
    private const uint VirtualTerminal = 0x0004;

    [DllImport("kernel32.dll")] private static extern nint GetStdHandle(int which);
    [DllImport("kernel32.dll")] private static extern bool GetConsoleMode(nint handle, out uint mode);
    [DllImport("kernel32.dll")] private static extern bool SetConsoleMode(nint handle, uint mode);

    /// <summary>Returns the mode to restore on leaving.</summary>
    public static uint Enter()
    {
        Console.OutputEncoding = new UTF8Encoding(false);
        var handle = GetStdHandle(StdOutput);
        GetConsoleMode(handle, out var mode);
        SetConsoleMode(handle, mode | VirtualTerminal);
        return mode;
    }

    public static void Leave(uint mode) => SetConsoleMode(GetStdHandle(StdOutput), mode);
}
