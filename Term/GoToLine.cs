namespace LeesBybel;

/// <summary>What is typed in the reference line. Tab completes the book's name. It
/// also searches: with a digit it is a reference, without one a search, since the AOV has no digits.</summary>
public sealed class GoToLine(BookNames names)
{
    private List<BookName>? _cycle;         // the books Tab is going through
    private int _at;

    public Field Field { get; } = new();

    public string Text => Field.Text;

    /// <summary>The rest of the book's name, in grey after the cursor at the end.</summary>
    public string Ghost => Field.AtEnd ? names.Ghost(Text) : "";

    public bool Searches => Text.Trim() != "" && !Text.Any(char.IsAsciiDigit);

    public Reference? Target => Searches ? null : names.Parse(Text);

    /// <summary>/ in an empty line closes it, as Esc.</summary>
    public bool Closes(ConsoleKeyInfo k) => k.KeyChar == '/' && Text == "";

    public void Key(ConsoleKeyInfo k)
    {
        if (k.Key == ConsoleKey.Tab)
        {
            Tab(k.Modifiers.HasFlag(ConsoleModifiers.Shift));
            return;
        }
        _cycle = null;
        Field.Key(k);
    }

    private void Tab(bool back)
    {
        if (_cycle is null)
        {
            var list = names.Completions(Text);
            if (list.Count == 0 || back) return;
            (_cycle, _at) = (list, 0);
        }
        else
        {
            var n = _cycle.Count;
            _at = (_at + (back ? n - 1 : 1)) % n;
        }
        Field.Set(_cycle[_at].Name + " ");
    }
}
