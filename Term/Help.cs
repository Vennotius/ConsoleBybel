namespace LeesBybel;

/// <summary>The keys, as a whole screen, in groups: each under its heading in the green of
/// "? hulp", the keys in light grey, a blank row between.</summary>
public static class Help
{
    private const int Gap = 3;              // between a key and what it does
    private const int Framing = 4;          // the title and the way back, with a row after and before

    private const string Title = "Sleutels";
    private const string Back = "Enige sleutel gaan terug na die teks";

    // A paired group's keys are short: two to a row.
    private static readonly (string? Heading, bool Paired, (string Key, string What)[] Keys)[] Groups =
    [
        ("Lees", false,
        [
            ("Op, Af", "Een ry op of af"),
            ("PgUp, PgDn, Spasie", "Een bladsy op of af"),
            ("Home, End", "Begin of einde van die boek"),
        ]),
        ("Gaan en soek", false,
        [
            ("/", "Gaan na ’n teks, of soek (sonder syfers)"),
            ("b", "Kies ’n boek en hoofstuk"),
            ("f", "Soek, of terug na die laaste soektog"),
            ("+ - = * |", "Met, sonder, presies, enige letters, of"),
        ]),
        ("In die soeklys", false,
        [
            ("b", "Kies boeke"),
            ("o", "Gelykes: verwysings of Bybelorde"),
        ]),
        ("Wys", true,
        [
            ("v", "Versnommers"), ("c", "Hoofstuknommers"),
            ("h", "Opskrifte"), ("s", "Statusbalk"),
            ("k", "Kleur"),
        ]),
        (null, false,
        [
            ("?", "Hierdie lys"),
            ("q", "Sluit"),
        ]),
    ];

    /// <summary>Too short a screen loses the title first (so 24 rows keep the headings), then the
    /// headings, then the blank rows, then the bottom rows; too narrow, the ends of the rows.</summary>
    public static void Draw(Screen s)
    {
        var all = Groups.Where(g => !g.Paired).SelectMany(g => g.Keys).ToList();
        var keyWidth = all.Max(k => k.Key.Length);
        var x = Math.Max((s.W - keyWidth - Gap - all.Max(k => k.What.Length)) / 2, 0);

        var keys = Groups.Sum(g => g.Paired ? (g.Keys.Length + 1) / 2 : g.Keys.Length);
        var headings = Groups.Count(g => g.Heading is not null);
        int Needed(bool heads, bool spaced, bool framed) =>
            keys + (heads ? headings : 0) + (spaced ? Groups.Length - 1 : 0) + (framed ? Framing : 0);
        var framed = s.H >= Needed(true, true, true);
        var heads = s.H >= Needed(true, true, framed);
        var spaced = s.H >= Needed(heads, true, framed);
        var total = Needed(heads, spaced, framed);
        var y = Math.Max((s.H - total) / 2, 0);

        if (framed)
        {
            Centred(s, y, Title, Attr.Help);
            Centred(s, y + total - 1, Back, Attr.Dim);
            y += 2;
        }
        for (var g = 0; g < Groups.Length; g++)
        {
            var (heading, paired, items) = Groups[g];
            if (g > 0 && spaced) y++;
            if (heads && heading is not null) s.Put(x, y++, heading, Attr.Help);
            if (!paired)
            {
                foreach (var (key, what) in items)
                {
                    s.Put(x, y, key, Attr.Soft);
                    s.Put(x + keyWidth + Gap, y++, what, Attr.Soft);
                }
                continue;
            }
            for (var i = 0; i < items.Length; i += 2, y++)
            {
                Pair(s, x, y, items[i]);
                if (i + 1 < items.Length) Pair(s, x + keyWidth + Gap, y, items[i + 1]);
            }
        }
    }

    /// <summary>"v  Versnommers".</summary>
    private static void Pair(Screen s, int x, int y, (string Key, string What) item) =>
        s.Put(x, y, item.Key + "  " + item.What, Attr.Soft);

    private static void Centred(Screen s, int y, string text, Attr a) =>
        s.Put(Math.Max((s.W - text.Length) / 2, 0), y, text, a);
}
