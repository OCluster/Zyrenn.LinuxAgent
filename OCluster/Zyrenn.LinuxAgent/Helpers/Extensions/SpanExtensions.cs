namespace Zyrenn.LinuxAgent.Helpers.Extensions;

public ref struct SpanSplitEnumerator
{
    #region Fields and properties region

    private bool _isFinished;
    private readonly char _separator;
    private ReadOnlySpan<char> _remaining;
    private readonly bool _removeEmptyEntries;
    public ReadOnlySpan<char> Current { get; private set; }

    #endregion

    #region Constructors region

    public SpanSplitEnumerator(ReadOnlySpan<char> buffer, 
        char separator, bool removeEmptyEntries = true)
    {
        _remaining = buffer;
        _separator = separator;
        _removeEmptyEntries = removeEmptyEntries;
        Current = default;
        _isFinished = buffer.IsEmpty;
    }

    #endregion

    #region Public methods region

    public SpanSplitEnumerator GetEnumerator() => this;

    public bool MoveNext()
    {
        if (_isFinished) return false;

        do
        {
            int index = _remaining.IndexOf(_separator);
            if (index < 0)
            {
                Current = _remaining;
                _remaining = default;
                _isFinished = true;
            }
            else
            {
                Current = _remaining.Slice(0, index);
                _remaining = _remaining.Slice(index + 1);
            }

            if (!_removeEmptyEntries || !Current.IsEmpty)
                return true;

        } while (!_isFinished);

        return false;
    }

    #endregion
}

public static class SpanExtensions
{
    #region Public methods region

    public static SpanSplitEnumerator SplitFast(
        this ReadOnlySpan<char> span, 
        char separator, 
        bool removeEmptyEntries = true)
    {
        return new SpanSplitEnumerator(span, separator, removeEmptyEntries);
    }

    #endregion
}
