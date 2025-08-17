namespace Zyrenn.LinuxAgent.Helpers.Extensions;

public ref struct SpanLineEnumerator
{
    #region Fields and properties region

    private ReadOnlySpan<char> _remaining;
    public ReadOnlySpan<char> Current { get; private set; }

    #endregion

    #region Constructors region

    public SpanLineEnumerator(ReadOnlySpan<char> buffer)
    {
        _remaining = buffer;
        Current = default;
    }

    #endregion

    #region Public methods region

    public SpanLineEnumerator GetEnumerator() => this;
    public bool MoveNext()
    {
        if (_remaining.IsEmpty) return false;

        int newLineIndex = _remaining.IndexOf('\n');
        if (newLineIndex == -1)
        {
            // Last line
            Current = _remaining;
            _remaining = default;
        }
        else
        {
            Current = _remaining.Slice(0, newLineIndex);
            _remaining = _remaining.Slice(newLineIndex + 1);
        }

        // Trim trailing carriage return if present
        if (!Current.IsEmpty && Current[^1] == '\r')
        {
            Current = Current.Slice(0, Current.Length - 1);
        }

        return true;
    }

    #endregion
}