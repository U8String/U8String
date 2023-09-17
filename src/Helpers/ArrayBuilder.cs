// Implementation by Neuecc from https://github.com/dotnet/runtime/pull/90459

namespace U8Primitives;

internal struct ToArrayHelper<T>
{
    [InlineArray(29)]
    private partial struct ArrayBlock
    {
    #pragma warning disable CA1823 // Avoid unused private fields
    #pragma warning disable IDE0044 // Add readonly modifier
    #pragma warning disable IDE0051 // Remove unused private members
        private T[] _array;
    #pragma warning restore IDE0051 // Remove unused private members
    #pragma warning restore IDE0044 // Add readonly modifier
    #pragma warning restore CA1823 // Avoid unused private fields
    }

    int _index;
    int _count;
    T[] _currentBlock;
    ArrayBlock _blocks;

    public ToArrayHelper(int initialCapacity)
    {
        _blocks = default;
        _currentBlock = _blocks[0] = new T[initialCapacity];
    }

    public Span<T> CurrentSpan => _currentBlock;

    public void AllocateNextBlock()
    {
        _index++;
        _count += _currentBlock.Length;

        int nextSize = unchecked(_currentBlock.Length * 2);
        if (nextSize < 0 || Array.MaxLength < (_count + nextSize))
        {
            nextSize = Array.MaxLength - _count;
        }

        _currentBlock = _blocks[_index] = new T[nextSize];
    }

    public T[] ToArray(int lastBlockCount)
    {
        T[] array = GC.AllocateUninitializedArray<T>(_count + lastBlockCount);
        Span<T> dest = array.AsSpan();
        for (int i = 0; i < _index; i++)
        {
            _blocks[i].CopyTo(dest);
            dest = dest.Slice(_blocks[i].Length);
        }
        _currentBlock.AsSpan(0, lastBlockCount).CopyTo(dest);
        return array;
    }
}
