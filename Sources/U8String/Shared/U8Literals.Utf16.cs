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

        // TODO: Consider full de-duplication at the expense of more expensive writes which do full comparison
        internal static byte[] GetLiteral([ConstantExpected] string value)
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
                ThrowHelpers.NonConstantString();
            }

            var length = Encoding.UTF8.GetByteCount(value);
            bytes = GC.AllocateArray<byte>(length + 1, pinned: true);

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
