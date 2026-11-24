namespace LeesBybel;

/// <summary>A verse found, or two side by side (<see cref="Reference.ToVerse"/>, "Johannes 3:16–17").
/// <see cref="Found"/> has the typed words' marks, in order, each in its word's colour. <see cref="Full"/>:
/// it has every typed word.</summary>
public sealed record Hit(Reference Ref, string Text, Mark[] Found, bool Full);

/// <summary>The search engine (<see cref="SearchEngine"/>) over the AOV, its results as the
/// console shows them.</summary>
public static class Search
{
    private static Corpus? _corpus;     // loaded by the first search

    /// <summary><paramref name="canonical"/>: equal scores in Bible order, not by cross-references.</summary>
    public static List<Hit> Run(BookNames names, string typed, bool canonical)
    {
        _corpus ??= Corpus.Load();
        var c = _corpus;
        var (terms, results) = SearchEngine.Search(c, typed, canonical);
        var shades = SearchEngine.Shades(terms);
        return results.Select(r =>
        {
            var v = c.Verses[r.V];
            var to = r.Pair && c.Verses[r.V + 1].AovVs != v.AovVs ? c.Verses[r.V + 1].AovVs : 0;  // not two parts of one verse
            var at = new Reference(names.Books[v.BookId - 1], v.AovCh, v.AovVs, to == 0 ? 0 : v.AovCh, to);
            var marks = SearchEngine.Spans(r.Matches, terms, shades)
                .Select(s => new Mark(s.Start, s.Length, Colour(s))).ToList();
            if (to == 0) return new Hit(at, r.Text.Replace('\n', ' '), [.. marks], r.Full);

            // A pair: the second verse's number where it starts, held to its first word.
            var cut = r.Text.IndexOf('\n');
            var number = to.ToString();
            var shift = number.Length + 1;
            marks = [.. marks.Select(m => m.At > cut ? m with { At = m.At + shift } : m),
                new Mark(cut + 1, number.Length, Attr.Dim)];
            var text = r.Text[..cut] + " " + number + Join + r.Text[(cut + 1)..];
            return new Hit(at, text, [.. marks.OrderBy(m => m.At)], r.Full);
        }).ToList();
    }

    /// <summary>Holds a pair's second verse number to its verse: a space the list won't wrap at.</summary>
    public const char Join = ' ';

    /// <summary>A whole word as typed (accents and all) has its colour behind it; one inside a
    /// longer word or with other accents, a form of it or a related word, on its letters.</summary>
    private static Attr Colour(Span s)
    {
        var part = !s.WholeWord || !s.DiacriticsExact || s.Kind != "word";
        if (s.Shade is not { } n) return part ? Attr.PartGrey : Attr.MarkGrey;
        return (Attr)((int)(part ? Attr.Part0 : Attr.Mark0) + n);
    }
}
