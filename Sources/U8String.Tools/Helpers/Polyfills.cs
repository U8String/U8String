namespace U8.Tools;

static class Polyfills
{
    internal static void Deconstruct<K, V>(
        this KeyValuePair<K, V> pair,
        out K key,
        out V value)
    {
        key = pair.Key;
        value = pair.Value;
    }
}