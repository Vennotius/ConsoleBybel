using System.Runtime.InteropServices;

namespace LeesBybel;

// The engine of CrossReferences.org's Bible search, cut to what this program uses. Its settings are in Scores.

// IsPhraseBonus: the whole unquoted query as a phrase; not required to match, adds its score when it does.
// IsNearBonus: not searched; adds the near bonus when the query words a verse has are close together (see GapScore).
// Must (+): only verses with it. Not (-): no verse with it. Exact (=): whole words only, no forms or related words.
// AsTyped (==): Exact, and case and diacritics as typed (' stands for the text's ’); searched in the text itself, not folded.
// A * in the text (lewe*, *heid, ge*nes, "God van Abra*m") stands for any letters of one word: Exact, every word whole
// with its stars filled in. A hit scores as a whole word.
// Either (vrees | bang): the words or phrases of which any one will do; together they count as one word.
public sealed record Term(string Text, bool IsPhrase, bool IsPhraseBonus = false, bool IsNearBonus = false,
    bool Must = false, bool Not = false, bool Exact = false, bool AsTyped = false,
    List<Term>? Either = null);

public sealed record TermInfo(string Text, string Folded, bool IsPhraseBonus, bool IsNearBonus, int Verses, double Weight = 1);

// Kind: "word" (the typed word, whole or part), "form" (books/forms.txt) or "related" (books/related.txt).
public readonly record struct MatchInfo(int Term, int Start, int Length, bool WholeWord, bool DiacriticsExact,
    bool CaseExact, int Score, string Kind = "word");

// A result: verse V, or the pair V, V + 1 (Text = both verses, joined by '\n').
// Full: it has every typed word.
public sealed record ResultItem(int V, bool Pair, string Text, List<MatchInfo> Matches, bool Full);

// A typed word's or phrase's match, overlapping ones merged, with its word's shade (null: grey) and the match's Kind.
public sealed record Span(int Start, int Length, bool WholeWord, bool DiacriticsExact, bool CaseExact, int? Shade,
    string Kind);

public static class SearchEngine
{
    /// <summary>Words separated by spaces; "quoted text" (straight or curly quotes) is one phrase term. Before either:
    /// + must be in the verse, - must not, = exactly this, == case and diacritics too (see Term; += and -= combine).
    /// lewe* starts like this, *heid ends like this, ge*nes does both. | joins the
    /// words or phrases left and right of it into one term (see Term.Either; + or - before the first covers it).
    /// A symbol alone is skipped.</summary>
    public static List<Term> Parse(string q)
    {
        static bool IsQuote(char ch) => ch is '"' or '“' or '”' or '„';
        var terms = new List<Term>();
        bool join = false;   // a | stands before this term
        void Add(Term t)
        {
            if (join) terms[^1] = Joined(terms[^1], t);
            else terms.Add(t);
            join = false;
        }
        int i = 0;
        while (i < q.Length)
        {
            if (char.IsWhiteSpace(q[i])) { i++; continue; }
            if (q[i] == '|') { join = terms.Count > 0; i++; continue; }
            bool must = false, not = false;
            int equals = 0;
            for (; i < q.Length && q[i] is ('+' or '-' or '='); i++)
            {
                must |= q[i] == '+';
                not |= q[i] == '-';
                if (q[i] == '=') equals++;
            }
            bool exact = equals > 0, asTyped = equals > 1;
            if (i == q.Length || char.IsWhiteSpace(q[i]) || q[i] == '|') continue;
            if (IsQuote(q[i]))
            {
                int end = i + 1;
                while (end < q.Length && !IsQuote(q[end])) end++;
                string p = string.Join(' ', q[(i + 1)..end].Split(' ',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                if (p.Length > 0) Add(new Term(p, true, Must: must, Not: not, Exact: exact || p.Contains('*'), AsTyped: asTyped));
                i = end + 1;
            }
            else
            {
                int end = i;
                while (end < q.Length && !char.IsWhiteSpace(q[end]) && !IsQuote(q[end]) && q[end] != '|') end++;
                string word = q[i..end];
                if (word.Any(ch => ch != '*')) Add(new Term(word, false, Must: must, Not: not, Exact: exact || word.Contains('*'), AsTyped: asTyped));
                i = end;
            }
        }
        return terms;
    }

    // + or - is the first one's.
    static Term Joined(Term a, Term b) => new(a.Text + " | " + b.Text, a.IsPhrase || b.IsPhrase, Must: a.Must, Not: a.Not,
        Either: [.. a.Either ?? [a], b]);

    sealed class Acc(int termCount)
    {
        public readonly int[] Best = new int[termCount];
        public readonly List<MatchInfo> Matches = [];
        public List<MatchInfo>? Related;   // related-word hits, kept only if near another word's match (KeepRelated)
    }

    // Rarity-weighted: all-word and partial matches (down to MinCoverage) in score order; equal scores by
    // blend70: the average of in70 (70% the In percentile, 30% the Out) and the percentile of a verse's
    // links (cross references either way) among the results.
    // When no verse has every typed word, two neighbouring verses of one chapter may match together (pairs).
    // canonical: equal scores in Bible order instead.
    public static (List<TermInfo> Terms, List<ResultItem> Results) Search(Corpus c, string q, bool canonical = false)
    {
        // Phrase-bonus hits running over a verse break, by the verse they start in (same chapter only).
        var cross = new Dictionary<int, List<MatchInfo>>();
        var parsed = Parse(q);
        var terms = parsed.Where(t => !t.Not).ToList();
        int words = terms.Count;   // the typed terms; bonus terms follow
        if (words >= 2 && terms.All(t => !t.IsPhrase))
        {
            if (terms.All(t => t.Either is null))
                terms.Add(new Term(string.Join(' ', terms.Select(t => t.Text)), true, IsPhraseBonus: true,
                    Exact: terms.Any(t => t.Text.Contains('*'))));
            terms.Add(new Term("(gaps, order)", false, IsNearBonus: true));
        }
        // A typed term equal to an earlier one finds the same matches: it is searched once (first[t] = that term).
        var first = new int[terms.Count];
        for (int t = 0; t < terms.Count; t++)
        {
            first[t] = t;
            if (t < words)
                for (int u = 0; u < t; u++)
                    if (terms[u] with { Must = terms[t].Must } == terms[t]) { first[t] = u; break; }
        }

        var acc = new Dictionary<int, Acc>();
        var infos = new List<TermInfo>();
        int[] starts = c.Starts;
        // The excluded terms (-) are searched last, into an acc of their own.
        int kept = terms.Count;
        terms.AddRange(parsed.Where(t => t.Not));
        var excluded = new Dictionary<int, Acc>();
        for (int t = 0; t < terms.Count; t++)
        {
            if (terms[t].IsNearBonus) continue;
            var found = t < kept ? acc : excluded;
            if (t < kept && first[t] != t) { infos.Add(infos[first[t]]); continue; }
            string term = terms[t].Text;
            int verses = 0;
            List<Term> ones = terms[t].Either ?? [terms[t]];
            foreach (var one in ones)
            {
                string text = one.Text;
                // A phrase is matched word by word, skipping punctuation between them; a single word as typed.
                // With a *, the words' pieces (ge*nes: ge, nes), in one list.
                var starred = text.Contains('*') ? Starred(text) : [];
                bool stars = starred.Count > 0;
                string[] tw = stars ? [.. starred.SelectMany(x => x.Pieces)]
                    : one.IsPhrase && Words(text) is { Length: > 0 } ws ? ws : [text];
                // As typed (==): the words themselves, searched in the text itself (as long as the folded text).
                string searched = one.AsTyped ? c.Text : c.Folded;
                string[] fw = [.. tw.Select(x => one.AsTyped ? x.Replace('\'', '’').Replace('‘', '’') : Fold.Apply(x))];
                int[] at = new int[tw.Length];
                bool caseListed = tw.Any(Scores.CaseWords.Contains);
                // Whole words only (null: any hit counts); a hit inside a listed word still counts. A phrase: its first
                // word at the start, its last at the end ("al nie vrees" not in "sal nie vrees").
                // An exact term (=, *) sets its own ends and uses no lists.
                HashSet<string>? onlyFirst = one.Exact ? null : OnlyFor(fw[0]), onlyLast = one.Exact ? null : OnlyFor(fw[^1]);
                // One hit per word ("almal": one al), the first unless a bracketed entry names another ("geen[een]").
                var marks = (one.Exact ? null : Scores.MarkAt.GetValueOrDefault(fw[0]))?.GetAlternateLookup<ReadOnlySpan<char>>();
                int pos = 0, p, v = 0, lastWord = -1;
                Acc? a = null;
                // Whether a word holding the first or last word is listed, worked out once per distinct word.
                var listedFirst = new Dictionary<string, bool>();
                var listedLast = new Dictionary<string, bool>();
                bool crossing = one.IsPhraseBonus;

                while ((p = searched.IndexOf(fw[0], pos, StringComparison.Ordinal)) >= 0)
                {
                    pos = p + 1;
                    if (!(stars ? FollowStarred(searched, p, starred, fw, at, crossing) : Follow(searched, p, fw, at, crossing))) continue;
                    int len = at[^1] + fw[^1].Length - p;
                    bool startOk = IsBoundary(c.Text, p - 1), endOk = IsBoundary(c.Text, p + len), ww = startOk && endOk;
                    if (one.Exact && !((startOk || stars && starred[0].OpenStart) && (endOk || stars && starred[^1].OpenEnd))) continue;
                    if (!startOk && onlyFirst is not null && !Allowed(c, p, onlyFirst, listedFirst)) continue;
                    if (!endOk && onlyLast is not null && !Allowed(c, at[^1], onlyLast, listedLast)) continue;
                    if (!ww)
                    {
                        int w0 = p;
                        while (!IsBoundary(c.Text, w0 - 1)) w0--;
                        if (w0 == lastWord) continue;
                        if (marks is { } mk && mk.TryGetValue(WordAt(c, p), out int off) && p - w0 != off) continue;
                        lastWord = w0;
                    }
                    ww |= one.Exact;
                    bool dia = true;
                    for (int k = 0; k < tw.Length && dia; k++) dia = SameIgnoringCase(c.Text.AsSpan(at[k], tw[k].Length), tw[k]);
                    bool cse = caseListed && dia && ListedWordsCaseExact(c.Text, at, tw);
                    int score = (ww ? (dia ? Scores.WholeExact : Scores.WholeFolded)
                                    : (dia ? Scores.SubExact : Scores.SubFolded))
                                + (cse ? Scores.CaseBonus : 0);

                    // Hits come in text order, so the verse only moves forward.
                    int v0 = v;
                    while (v + 1 < starts.Length && starts[v + 1] <= p) v++;
                    if (crossing && v + 1 < starts.Length && p + len > starts[v + 1])
                    {
                        // Over a break: kept for the pair v, v+1 only if it ends in v+1 and both are one chapter.
                        if ((v + 2 >= starts.Length || p + len < starts[v + 2]) && SameChapter(c, v))
                        {
                            if (!cross.TryGetValue(v, out var cl)) cross[v] = cl = [];
                            cl.Add(new MatchInfo(t, p - starts[v], len, ww, dia, cse, score));
                        }
                        a = null;   // the next hit looks its verse up again
                        continue;
                    }
                    if (a is null || v != v0) { if (!found.TryGetValue(v, out a)) found[v] = a = new Acc(terms.Count); }
                    if (a.Best[t] == 0) verses++;
                    a.Best[t] = Math.Max(a.Best[t], score);
                    a.Matches.Add(new MatchInfo(t, p - starts[v], len, ww, dia, cse, score));
                }
            }
            // After all the words, so Verses counts the words themselves.
            foreach (var one in ones)
            {
                if (one.IsPhrase || one.Exact) continue;
                string f = Fold.Apply(one.Text);
                if (c.Forms.TryGetValue(f, out var related))
                    foreach (var (form, score) in related) ScanForm(c, form, score, t, terms.Count, found);
                if (t < kept && c.Related.TryGetValue(f, out var near))
                    foreach (var (r, score) in near) ScanForm(c, r, score, t, terms.Count, acc, related: true);
            }
            if (t >= kept) continue;
            infos.Add(new TermInfo(term, Fold.Apply(term), terms[t].IsPhraseBonus, false, verses));
        }
        terms.RemoveRange(kept, terms.Count - kept);
        foreach (int v in excluded.Keys) acc.Remove(v);   // so no pair holds them either
        KeepRelated(c, acc, words);
        // The copies take their first's best scores.
        bool copies = false;
        for (int t = 0; t < words; t++) copies |= first[t] != t;
        if (copies)
            foreach (var a in acc.Values)
                for (int t = 0; t < words; t++) a.Best[t] = a.Best[first[t]];

        int nearIdx = terms.FindIndex(t => t.IsNearBonus);
        // The near bonus is checked below, only for the verses that pass coverage.
        if (nearIdx >= 0) infos.Add(new TermInfo(terms[nearIdx].Text, "", false, true, 0));
        var gapBuf = new List<(int Start, int End, int Term)>();

        double[] w = Weights(infos, words, c.Verses.Length);
        for (int t = 0; t < infos.Count; t++) infos[t] = infos[t] with { Weight = Math.Round(w[t], 2) };
        double totalW = w.Take(words).Sum();
        // How often each word was typed ("EK IS WAT EK IS": EK and IS twice); the copies find the same occurrences.
        int[] typed = infos.Select((i, t) => t < words ? infos.Take(words).Count(u => u.Folded == i.Folded) : 1).ToArray();

        bool HasMust(Acc a) { for (int t = 0; t < words; t++) if (terms[t].Must && a.Best[t] == 0) return false; return true; }
        var hits = new List<(int v, Acc a, double[] ts, double score, double cov, bool pair)>();
        foreach (var (v, a) in acc)
        {
            double cov = totalW > 0 ? Coverage(a.Best, w, words, totalW) : 1;
            if (!HasMust(a)) continue;
            if (cov < Scores.MinCoverage) continue;
            var ts = TermScores(a.Matches, terms.Count, typed, first, words);
            if (nearIdx >= 0) ts[nearIdx] = GapScore(c.VerseText(v), a.Matches, words, gapBuf, w);
            for (int t = 0; t < ts.Length; t++) ts[t] = Math.Round(ts[t] * w[t], 1);
            hits.Add((v, a, ts, Math.Round(ts.Sum(), 1), cov, false));
        }
        // Pairs: when no verse has every typed word, two neighbouring verses of one chapter are scored as one
        // text (verse n, '\n', verse n+1), the phrase and near bonus running over the break. Like a verse, a pair may
        // miss words (coverage >= minCoverage); each verse adds a typed word the other lacks.
        if (totalW > 0 && !hits.Any(h => h.cov == 1))
            foreach (var (v, a) in acc.ToList())
            {
                if (!acc.TryGetValue(v + 1, out var b) || !SameChapter(c, v)) continue;
                var m = new Acc(terms.Count);
                for (int t = 0; t < terms.Count; t++) m.Best[t] = Math.Max(a.Best[t], b.Best[t]);
                double pc = Coverage(m.Best, w, words, totalW);
                if (pc < Scores.MinCoverage || !HasMust(m) || !Adds(a, b, words) || !Adds(b, a, words)) continue;
                int off = starts[v + 1] - starts[v];
                m.Matches.AddRange(a.Matches);
                m.Matches.AddRange(b.Matches.Select(x => x with { Start = x.Start + off }));
                if (cross.TryGetValue(v, out var cl))
                    foreach (var x in cl) { m.Matches.Add(x); m.Best[x.Term] = Math.Max(m.Best[x.Term], x.Score); }
                m.Matches.Sort((x, y) => x.Term != y.Term ? x.Term.CompareTo(y.Term) : x.Start.CompareTo(y.Start));
                string text = PairText(c, v);
                var ts = TermScores(m.Matches, terms.Count, typed, first, words);
                if (nearIdx >= 0) ts[nearIdx] = GapScore(text, m.Matches, words, gapBuf, w);
                // Chance: a word only the weaker verse has counts only as far as
                // it is unlikely to be in a neighbouring verse by chance (1 - share of verses holding it).
                bool aBetter = Coverage(a.Best, w, words, totalW) >= Coverage(b.Best, w, words, totalW);
                double of = PairOrderFactor(a, b, words, first, Scores.PairOrderFloor);
                for (int t = 0; t < ts.Length; t++)
                {
                    ts[t] *= w[t] * of;
                    if (t < words && (aBetter ? a.Best[t] == 0 : b.Best[t] == 0))
                        ts[t] *= 1 - (double)infos[t].Verses / c.Verses.Length;
                    ts[t] = Math.Round(ts[t], 1);
                }
                hits.Add((v, m, ts, Math.Round(ts.Sum(), 1), pc, true));
            }
        // Coverage: a pair stays only if it covers more of the query than the best verse.
        if (hits.Any(h => !h.pair))
        {
            double topCov = hits.Where(h => !h.pair).Max(h => h.cov);
            hits.RemoveAll(h => h.pair && h.cov <= topCov + 1e-9);
        }
        var setLinks = new int[c.Verses.Length];
        var inSet = new bool[c.Verses.Length];
        foreach (var h in hits) inSet[h.v] = true;
        foreach (var h in hits) foreach (int u in c.XrefLinks[h.v]) if (inSet[u]) setLinks[h.v]++;
        // percentile of a verse's links among the results: the share of results with fewer
        var sorted = hits.Select(h => setLinks[h.v]).Order().ToArray();
        var firstAt = new Dictionary<int, int>();
        for (int i = sorted.Length - 1; i >= 0; i--) firstAt[sorted[i]] = i;
        var tb = new double[c.Verses.Length];
        foreach (var h in hits) tb[h.v] = (c.XrefIn70[h.v] + 100.0 * firstAt[setLinks[h.v]] / sorted.Length) / 2;
        hits.Sort((x, y) =>
        {
            int r = y.score.CompareTo(x.score);
            if (r == 0 && !canonical) r = tb[y.v].CompareTo(tb[x.v]);   // equal scores: better-referenced first
            return r != 0 ? r : x.v.CompareTo(y.v);
        });
        // A verse appears once, in the first result that holds it: a verse in a
        // pair is not listed again alone or in a lower pair.
        if (hits.Any(h => h.pair))
        {
            var shown = new HashSet<int>();
            hits = hits.Where(h => h.pair ? !shown.Contains(h.v) && !shown.Contains(h.v + 1) && shown.Add(h.v) && shown.Add(h.v + 1)
                                          : shown.Add(h.v)).ToList();
        }
        var results = hits.Select(x => new ResultItem(x.v, x.pair, x.pair ? PairText(c, x.v) : c.VerseText(x.v),
            WithCopies(x.a.Matches, first, words), Enumerable.Range(0, words).All(t => x.a.Best[t] > 0))).ToList();
        return (infos, results);
    }

    // A verse's matches in text order, each copy of a term listed too (equal starts in term order).
    static List<MatchInfo> WithCopies(List<MatchInfo> matches, int[] first, int words)
    {
        var all = new List<MatchInfo>(matches);
        for (int t = 0; t < words; t++)
            if (first[t] != t)
                foreach (var m in matches) if (m.Term == first[t]) all.Add(m with { Term = t });
        all.Sort((x, y) => x.Start != y.Start ? x.Start.CompareTo(y.Start) : x.Term.CompareTo(y.Term));
        return all;
    }

    // Rarer words weigh more. idf = ln(verses / verses the term hits), scaled so the rarest typed
    // word weighs 1 and a word in every verse weighs WeightFloor. A word with no hits weighs 0, so it doesn't
    // count against coverage. Bonus terms get the mean weight, keeping their proportion to the words.
    static double[] Weights(List<TermInfo> infos, int words, int verseCount)
    {
        var w = new double[infos.Count];
        var idf = infos.Take(words).Select(i => i.Verses > 0 ? Math.Log((double)verseCount / i.Verses) : 0).ToArray();
        double max = idf.DefaultIfEmpty().Max();   // no words typed (a query of only `""`): no results
        for (int t = 0; t < words; t++)
            w[t] = infos[t].Verses == 0 ? 0
                 : max > 0 ? Scores.WeightFloor + (1 - Scores.WeightFloor) * idf[t] / max : 1;
        double mean = w.Take(words).Where(x => x > 0).DefaultIfEmpty(1).Average();
        for (int t = words; t < w.Length; t++) w[t] = mean;
        return w;
    }

    // The share of the query's weight a verse has. A verse with every word has 1 (its score ranks it
    // among the others). In a partial match a word found only inside a longer word counts for SubExact/WholeExact
    // of its weight ("regsaak" is 0.4 of reg), so a fragment doesn't lift a verse above ones with whole words.
    // Diacritics don't count here: typing "oe" for oë is normal.
    static double Coverage(int[] best, double[] w, int words, double totalW)
    {
        bool all = true;
        for (int t = 0; t < words && all; t++) all = w[t] == 0 || best[t] > 0;
        if (all) return 1;
        double part = (double)Scores.SubExact / Scores.WholeExact, sum = 0;
        for (int t = 0; t < words; t++) sum += best[t] >= Scores.WholeFolded ? w[t] : best[t] > 0 ? w[t] * part : 0;
        return Math.Round(sum / totalW, 3);
    }

    // Occurrences of a term in a verse, best score first (rank b): a word typed n times
    // (typed[t] = n) gets its n best hits in full; the k-th hit beyond them adds its score × RepeatFactor^k
    // (now 0: nothing). Whole-word and partial hits in one list, best first. The total is split over the n
    // copies of the term.
    // A verse's matches come term by term (the search adds them so), so each term's are one run.
    static double[] TermScores(List<MatchInfo> matches, int termCount, int[] typed, int[] first, int words)
    {
        var result = new double[termCount];
        var span = CollectionsMarshal.AsSpan(matches);
        for (int i = 0; i < span.Length;)
        {
            int t = span[i].Term, j = i;
            while (j < span.Length && span[j].Term == t) j++;
            var ms = span[i..j];
            i = j;
            var s = new int[ms.Length];
            for (int k = 0; k < ms.Length; k++) s[k] = ms[k].Score;
            Array.Sort(s); Array.Reverse(s);
            int n = typed[t];
            double v = 0;
            for (int k = 0; k < s.Length; k++) v += k < n ? s[k] : s[k] * Math.Pow(Scores.RepeatFactor, k - n + 1);
            v /= n;   // the copies share it: typed twice, found once = 100 + 0, so 50 each
            result[t] = Math.Round(v, 1);
        }
        for (int t = 0; t < words; t++) result[t] = result[first[t]];
        return result;
    }

    // Proximity by groups: a group is a run of query
    // words (whole words) with at most NearGap words between neighbours; a full stop, ? ! or ; ends it. Each step
    // scores 1, 0.67, 0.33 for 0, 1, 2 words between; a step against the typed order counts NearAgainst
    // of that. A group of n steps scores (b^n - 1) / (b - 1), b = NearCurve: a pair 1, three words side by side
    // 1 + b, four 1 + b + b². The best group is taken first and its words used; then the best of the words left,
    // and so on. Each counts by its words' mean weight against the query's mean; the sum is at most NearCap pairs.
    // NearBonus × that. A repeat inside a group is skipped (it counts as a word between).
    static double GapScore(string text, List<MatchInfo> matches, int words, List<(int Start, int End, int Term)> occ,
        double[] w)
    {
        occ.Clear();
        foreach (var m in matches)
            if (m.WholeWord && m.Term < words) occ.Add((m.Start, m.Start + m.Length, m.Term));
        occ.Sort((x, y) => x.Start != y.Start ? x.Start.CompareTo(y.Start) : x.Term.CompareTo(y.Term));
        // One occurrence per place: two typed terms can find the same word, or one inside the other ("Bet-El El").
        for (int i = 1; i < occ.Count; ) if (occ[i].Start < occ[i - 1].End) occ.RemoveAt(i); else i++;
        var used = new HashSet<int>();
        double total = 0, steps = Scores.NearGap + 1, b = Scores.NearCurve;
        while (true)
        {
            double bestV = 0, n = 0;
            HashSet<int>? bestTerms = null;
            var run = new HashSet<int>();
            int prev = -1;
            for (int i = 0; i < occ.Count; i++)
            {
                var o = occ[i];
                if (used.Contains(o.Term)) continue;
                double gap = prev < 0 ? steps : WordsBetween(text, occ[prev].End, o.Start);
                if (prev >= 0 && text.AsSpan(occ[prev].End, o.Start - occ[prev].End).ContainsAny(".?!;")) gap = steps;
                if (gap > Scores.NearGap) { run = [o.Term]; n = 0; prev = i; continue; }
                if (run.Contains(o.Term)) continue;
                double step = (steps - gap) / steps;
                n += o.Term < occ[prev].Term ? Scores.NearAgainst * step : step;
                run.Add(o.Term);
                prev = i;
                double v = (Math.Pow(b, n) - 1) / (b - 1);
                if (v > bestV) (bestV, bestTerms) = (v, [.. run]);
            }
            if (bestTerms is null) break;
            total += bestTerms.Average(t => w[t]) / w[words] * bestV;
            used.UnionWith(bestTerms);
        }
        return Scores.NearBonus * Math.Min(total, Scores.NearCap);
    }

    // A word form (Scores.FormScores) of term t, as a whole word anywhere. Its hits count for the score, coverage and
    // nearness like the typed word's, but not in the word's Verses: the weight stays the typed word's.
    // related: a related word's hit, put aside for KeepRelated.
    static void ScanForm(Corpus c, string form, int score, int t, int termCount, Dictionary<int, Acc> acc,
        bool related = false)
    {
        string f = Fold.Apply(form);
        int pos = 0, p;
        while ((p = c.Folded.IndexOf(f, pos, StringComparison.Ordinal)) >= 0)
        {
            pos = p + 1;
            if (!IsBoundary(c.Text, p - 1) || !IsBoundary(c.Text, p + f.Length)) continue;
            bool dia = SameIgnoringCase(c.Text.AsSpan(p, f.Length), form);
            int s = dia ? score : score * Scores.WholeFolded / Scores.WholeExact;
            int v = c.VerseIndexAt(p);
            if (!acc.TryGetValue(v, out var a)) acc[v] = a = new Acc(termCount);
            var m = new MatchInfo(t, p - c.Starts[v], f.Length, true, dia, false, s, Kind: related ? "related" : "form");
            if (related) { (a.Related ??= []).Add(m); continue; }
            a.Best[t] = Math.Max(a.Best[t], s);
            a.Matches.Add(m);
        }
    }

    // A related word's hit counts only next to a whole-word match (exact or form) of another query word: at most
    // NearGap words between, no . ? ! ; between. The rest are dropped, and a verse left with
    // no match at all with them.
    static void KeepRelated(Corpus c, Dictionary<int, Acc> acc, int words)
    {
        foreach (var (v, a) in acc.ToList())
        {
            if (a.Related is null) continue;
            string text = c.VerseText(v);
            bool kept = false;
            int own = a.Matches.Count;   // only these count as neighbours, not another kept related hit
            foreach (var r in a.Related)
            {
                for (int i = 0; i < own; i++)
                {
                    var m = a.Matches[i];
                    if (!m.WholeWord || m.Term >= words || m.Term == r.Term) continue;
                    var (from, to) = m.Start < r.Start ? (m.Start + m.Length, r.Start) : (r.Start + r.Length, m.Start);
                    if (from > to || WordsBetween(text, from, to) > Scores.NearGap
                        || text.AsSpan(from, to - from).ContainsAny(".?!;")) continue;
                    a.Best[r.Term] = Math.Max(a.Best[r.Term], r.Score);
                    a.Matches.Add(r);
                    kept = true;
                    break;
                }
            }
            a.Related = null;
            if (a.Matches.Count == 0) acc.Remove(v);
            else if (kept)
            {
                // TermScores reads each term's matches as one block.
                var sorted = a.Matches.OrderBy(x => x.Term).ToList();
                a.Matches.Clear();
                a.Matches.AddRange(sorted);
            }
        }
    }

    static int WordsBetween(string text, int from, int to)
    {
        int n = 0;
        bool inWord = false;
        for (int i = from; i < to; i++)
        {
            bool w = char.IsLetterOrDigit(text[i]);
            if (w && !inWord) n++;
            inWord = w;
        }
        return n;
    }

    // A phrase's words: letters and digits, split at spaces and punctuation ("hef ’n klaaglied": hef, n, klaaglied).
    static string[] Words(string s) =>
        s.Split([.. s.Where(ch => !char.IsLetterOrDigit(ch)).Distinct()], StringSplitOptions.RemoveEmptyEntries);

    // Words 1.. follow the word at p in order, each after spaces or punctuation only, within the verse
    // ("herder niks" finds "herder; niks"). Fills `at` with where each word starts.
    // cross (the phrase bonus, for pairs): the words may also run over a verse break.
    static bool Follow(string folded, int p, string[] words, int[] at, bool cross)
    {
        at[0] = p;
        int i = p + words[0].Length;
        for (int k = 1; k < words.Length; k++)
        {
            int j = i;
            while (j < folded.Length && (cross || folded[j] != '\n') && !char.IsLetterOrDigit(folded[j])) j++;
            if (j == i || string.CompareOrdinal(folded, j, words[k], 0, words[k].Length) != 0) return false;
            at[k] = j;
            i = j + words[k].Length;
        }
        return true;
    }

    // A word with stars: ge*nes has the pieces ge and nes; OpenStart / OpenEnd: a star at that end.
    sealed record StarWord(string[] Pieces, bool OpenStart, bool OpenEnd);

    // The words of a text with stars. A lone * is any one word between two others; first or last it is dropped.
    static List<StarWord> Starred(string text)
    {
        var words = text.Split([.. text.Where(ch => ch != '*' && !char.IsLetterOrDigit(ch)).Distinct()], StringSplitOptions.RemoveEmptyEntries)
            .Select(w => new StarWord(w.Split('*', StringSplitOptions.RemoveEmptyEntries), w[0] == '*', w[^1] == '*')).ToList();
        while (words.Count > 0 && words[0].Pieces.Length == 0) words.RemoveAt(0);
        while (words.Count > 0 && words[^1].Pieces.Length == 0) words.RemoveAt(words.Count - 1);
        return words;
    }

    // Like Follow, for words with stars: the first one's first piece stands at p, and each word fills its word in the
    // text. text: folded, or as it is for ==; pieces: all the words' pieces, folded alike; at: where each starts.
    static bool FollowStarred(string text, int p, List<StarWord> words, string[] pieces, int[] at, bool cross)
    {
        int start = p, k = 0;
        for (int w = 0; w < words.Count; w++)
        {
            int end = start;
            while (!IsBoundary(text, end)) end++;
            int i = start;
            for (int n = 0; n < words[w].Pieces.Length; n++, k++)
            {
                bool atStart = n == 0 && (w == 0 || !words[w].OpenStart);
                if (!atStart && n == words[w].Pieces.Length - 1 && !words[w].OpenEnd) i = Math.Max(i, end - pieces[k].Length);
                int j = atStart ? i : text.IndexOf(pieces[k], i, end - i, StringComparison.Ordinal);
                if (j < 0 || j + pieces[k].Length > end || string.CompareOrdinal(text, j, pieces[k], 0, pieces[k].Length) != 0) return false;
                at[k] = j;
                i = j + pieces[k].Length;
            }
            if (!words[w].OpenEnd && i != end) return false;
            if (w == words.Count - 1) return true;
            start = end;
            while (start < text.Length && (cross || text[start] != '\n') && IsBoundary(text, start)) start++;
            if (start == end || IsBoundary(text, start)) return false;
        }
        return true;
    }

    // Pairs: 1 when every typed word only the first verse has comes before every one only the second has;
    // down to floor when all such cross pairs are inverted (copies of a typed word count once).
    static double PairOrderFactor(Acc a, Acc b, int words, int[] first, double floor)
    {
        int cross = 0, inv = 0;
        for (int i = 0; i < words; i++)
        {
            if (first[i] != i || a.Best[i] == 0 || b.Best[i] > 0) continue;
            for (int j = 0; j < words; j++)
            {
                if (first[j] != j || b.Best[j] == 0 || a.Best[j] > 0) continue;
                cross++; if (i > j) inv++;
            }
        }
        return cross == 0 ? 1 : 1 - (1 - floor) * inv / cross;
    }

    static bool Adds(Acc x, Acc y, int words)
    {
        for (int t = 0; t < words; t++) if (x.Best[t] > 0 && y.Best[t] == 0) return true;
        return false;
    }

    static bool SameChapter(Corpus c, int v) => v + 1 < c.Verses.Length
        && c.Verses[v].BookId == c.Verses[v + 1].BookId && c.Verses[v].AovCh == c.Verses[v + 1].AovCh;

    static string PairText(Corpus c, int v) => c.VerseText(v) + "\n" + c.VerseText(v + 1);

    static bool IsBoundary(string s, int i) => i < 0 || i >= s.Length || !char.IsLetterOrDigit(s[i]);

    // The whole-word-only entry for a folded word (one letter: whole words only, no exceptions); null: none.
    static HashSet<string>? OnlyFor(string folded) =>
        folded.Length == 1 ? [] : Scores.WholeWordOnly.GetValueOrDefault(folded);

    // Whether the word around p is on the list, cached per distinct word.
    static bool Allowed(Corpus c, int p, HashSet<string> only, Dictionary<string, bool> cache)
    {
        if (only.Count == 0) return false;
        var word = WordAt(c, p);
        var lookup = cache.GetAlternateLookup<ReadOnlySpan<char>>();
        if (!lookup.TryGetValue(word, out bool ok))
        {
            string name = word.ToString();
            cache[name] = ok = Scores.Listed(only, name);
        }
        return ok;
    }

    // The whole word (folded) around position p.
    static ReadOnlySpan<char> WordAt(Corpus c, int p)
    {
        int s = p, e = p;
        while (!IsBoundary(c.Text, s - 1)) s--;
        while (!IsBoundary(c.Text, e)) e++;
        return c.Folded.AsSpan(s, e - s);
    }

    // Case is compared only for the words in Scores.CaseWords ("Die HERE sê": only HERE counts).
    // At a sentence start a capital says nothing (nor do the AOV's all-caps pericope starts, "HY", "EK",
    // which always stand there), so such a word gets no case bonus, whatever case was typed.
    // HERE is the exception: there the capitals still mean the divine name.
    static bool ListedWordsCaseExact(string text, int[] at, string[] words)
    {
        for (int k = 0; k < words.Length; k++)
            if (Scores.CaseWords.Contains(words[k]) &&
                (!text.AsSpan(at[k], words[k].Length).SequenceEqual(words[k]) ||
                 !words[k].Equals("HERE", StringComparison.OrdinalIgnoreCase) && SentenceStart(text, at[k])))
                return false;
        return true;
    }

    // A sentence starts after . ! ? or : (the AOV starts speech after a colon with a capital), also across
    // verses: a verse that continues the previous one's sentence ("…, Hy") is not a start. The word after
    // a sentence-initial ’n counts too, since ’n is never capitalised ("’n Koning", "’N NAAM").
    static bool SentenceStart(string text, int i)
    {
        int j = i - 1;
        while (j >= 0 && (char.IsWhiteSpace(text[j]) || text[j] is '(' or ')' or '"' or '„' or '“' or '”')) j--;
        if (j < 0 || text[j] is '.' or '!' or '?' or ':') return true;
        return j >= 1 && text[j] is 'n' or 'N' && text[j - 1] is '’' or '\'' && IsBoundary(text, j - 2)
            && SentenceStart(text, j - 1);
    }

    // Equal apart from case (and ’ vs '): i.e. the diacritics are as typed.
    static bool SameIgnoringCase(ReadOnlySpan<char> a, string b)
    {
        for (int i = 0; i < a.Length; i++)
            if (Norm(a[i]) != Norm(b[i])) return false;
        return true;
        static char Norm(char c) => c is '’' or '‘' ? '\'' : char.ToLowerInvariant(c);
    }

    // Each typed word once (a word typed twice, or folding alike, shares one), by weight, ties
    // in typed order; the first Scores.Shades not under Scores.GreyUnder get a shade 0, 1, ...; the rest null
    // (grey). Bonus terms none. By term.
    public static int?[] Shades(List<TermInfo> infos)
    {
        var byTerm = new int?[infos.Count];
        var words = infos.Select((t, i) => (t, i)).Where(x => !x.t.IsPhraseBonus && !x.t.IsNearBonus)
            .GroupBy(x => x.t.Folded).OrderByDescending(g => g.First().t.Weight).ThenBy(g => g.First().i).ToList();
        int next = 0;
        foreach (var g in words)
        {
            var t = g.First().t;
            int? shade = t.Weight < Scores.GreyUnder || next >= Scores.Shades ? null : next++;
            foreach (var x in g) byTerm[x.i] = shade;
        }
        return byTerm;
    }

    // The typed words' and phrases' matches (not the phrase bonus, whose words are matched on
    // their own), in order, overlapping ones merged, so they can be marked without checking overlaps.
    // A merged span carries the kind (whole word, diacritics, case) and shade of its best-scoring match.
    public static List<Span> Spans(List<MatchInfo> matches, List<TermInfo> terms, int?[] shades)
    {
        var spans = new List<Span>();
        int best = 0;
        foreach (var m in matches.Where(m => !terms[m.Term].IsPhraseBonus).OrderBy(m => m.Start))
        {
            if (spans.Count > 0 && spans[^1] is var last && m.Start < last.Start + last.Length)
            {
                int end = Math.Max(last.Start + last.Length, m.Start + m.Length);
                spans[^1] = m.Score > best
                    ? new Span(last.Start, end - last.Start, m.WholeWord, m.DiacriticsExact, m.CaseExact, shades[m.Term], m.Kind)
                    : last with { Length = end - last.Start };
                best = Math.Max(best, m.Score);
            }
            else
            {
                spans.Add(new Span(m.Start, m.Length, m.WholeWord, m.DiacriticsExact, m.CaseExact, shades[m.Term], m.Kind));
                best = m.Score;
            }
        }
        return spans;
    }
}
