namespace U8;

/// <summary>
/// default(T) must be a valid value/state for the type T.
/// </summary>
/// <typeparam name="T"></typeparam>
internal sealed record Box<T> where T : struct
{
    T _value;

    public ref T Ref => ref _value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Box(T value)
    {
        _value = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Box()
    {
        _value = new T();
    }
}