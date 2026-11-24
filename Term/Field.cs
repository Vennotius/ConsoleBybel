namespace LeesBybel;

/// <summary>A line of typing with a cursor: Left, Right, Home and End move it; typing, Backspace
/// and Delete work where it is.</summary>
public sealed class Field
{
    public string Text { get; private set; } = "";
    public int At { get; private set; }

    public bool AtEnd => At == Text.Length;

    /// <summary>All of it replaced, the cursor at the end.</summary>
    public void Set(string text) => (Text, At) = (text, text.Length);

    public void Key(ConsoleKeyInfo k)
    {
        switch (k.Key)
        {
            case ConsoleKey.LeftArrow: At = Math.Max(At - 1, 0); return;
            case ConsoleKey.RightArrow: At = Math.Min(At + 1, Text.Length); return;
            case ConsoleKey.Home: At = 0; return;
            case ConsoleKey.End: At = Text.Length; return;
            case ConsoleKey.Backspace:
                if (At > 0) (Text, At) = (Text.Remove(At - 1, 1), At - 1);
                return;
            case ConsoleKey.Delete:
                if (At < Text.Length) Text = Text.Remove(At, 1);
                return;
        }
        if (!char.IsControl(k.KeyChar)) (Text, At) = (Text.Insert(At, k.KeyChar.ToString()), At + 1);
    }
}
