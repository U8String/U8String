using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Unicode;

namespace U8.Shared;

static partial class U8Literals
{
    internal static class Utf16
    {
        readonly static ConditionalWeakTable<string, byte[]> LiteralPool = [];

        internal static unsafe byte[] GetLiteral([ConstantExpected] string value)
        {
            Debug.Assert(!string.IsNullOrEmpty(value));

            var pool = LiteralPool;
            if (pool.TryGetValue(value, out var bytes))
            {
                return bytes;
            }

            var gcgen = GC.GetGeneration(value);
            if (gcgen != int.MaxValue)
            {
                ThrowHelpers.ArgumentException();
            }

            var length = Encoding.UTF8.GetByteCount(value);
            bytes = new byte[length + 1];

            var result = Utf8.FromUtf16(
               source: value,
               destination: bytes,
               charsRead: out _,
               bytesWritten: out _,
               replaceInvalidSequences: false,
               isFinalBlock: true);

            if (result != OperationStatus.Done)
            {
                ThrowHelpers.InvalidUtf8();
            }

            pool.AddOrUpdate(value, bytes);
            return bytes;
        }
    }
}
