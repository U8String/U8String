using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace U8.Tools.Generators.OptimizationScopes;

// TODO:
// U8Builder:
// - Append
// - AppendLine
// - AppendLiteral
// InterpolatedU8StringHandler: (is this even possible?)
// - AppendLiteral
// - AppendFormatted

sealed class FoldConversions : IOptimizationScope
{
    static readonly UTF8Encoding UTF8 = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    static readonly string[] ByteLookup = Enumerable
        .Range(0, 256)
        .Select(i => i.ToString(CultureInfo.InvariantCulture))
        .ToArray();

    readonly List<string> _literalValues = [];
    readonly Dictionary<object, Interceptor> _literalMap = new(new LiteralComparer());

    public string Name => "Literals";

    public IEnumerable<string> Imports =>
    [
        "System", "System.Runtime.CompilerServices",
        "U8", "U8.InteropServices"
    ];

    public IEnumerable<string> Fields => _literalValues;

    public IEnumerable<Interceptor> Interceptors => _literalMap.Values;

    static bool IsSupportedMethod(IMethodSymbol method)
    {
        var mehodName = method.Name;
        var containingType = method.ContainingType.Name;

        return (containingType, mehodName) switch
        {
            ("U8String", "Create") => true,
            ("U8String", "CreateLossy") => true,
            ("U8String", "CreateInterned") => true,
            ("U8String", "FromAscii") => true,
            ("U8String", "FromLiteral") => true,

            ("Syntax", "u8") => true,

            _ => false
        };
    }

    public bool ProcessCallsite(
        SemanticModel model,
        IMethodSymbol method,
        InvocationExpressionSyntax invocation)
    {
        if (!IsSupportedMethod(method) ||
            invocation.ArgumentList.Arguments is not [var argument])
        {
            return false;
        }

        var constant = model.GetConstantValue(argument.Expression);
        if (constant is not { HasValue: true, Value: object constantValue })
        {
            return false;
        }

        var callsite = new Callsite(method, invocation);
        if (_literalMap.TryGetValue(constantValue, out var interceptor))
        {
            // Already known literal - append a callsite and return
            interceptor.Callsites.Add(callsite);
            return true;
        }

        if (!TryGetString(constantValue, out var utf16) ||
            !TryGetBytes(utf16, out var utf8))
        {
            return false;
        }

        var argType = constantValue.GetType().Name;
        var literalName = AddByteLiteral(utf8, utf16[^1] != 0);

        _literalMap[constantValue] = new(
            Method: method,
            InstanceArg: null,
            GenericArgs: IsGenericOverload(constantValue) ? [argType] : [],
            CustomAttrs: [Constants.AggressiveInlining],
            Callsites: [callsite],
            Body: $"return U8Marshal.CreateUnsafe(_{literalName}, 0, {utf8.Length});");

        return true;
    }

    static bool IsGenericOverload(object value)
    {
        return value is not (string or bool or byte);
    }

    static bool TryGetString(object? value, [NotNullWhen(true)] out string? result)
    {
        var utf16 = value switch
        {
            byte u8 => u8.ToString(CultureInfo.InvariantCulture),
            sbyte i8 => i8.ToString(CultureInfo.InvariantCulture),
            ushort u16 => u16.ToString(CultureInfo.InvariantCulture),
            short i16 => i16.ToString(CultureInfo.InvariantCulture),
            uint u32 => u32.ToString(CultureInfo.InvariantCulture),
            int i32 => i32.ToString(CultureInfo.InvariantCulture),
            ulong u64 => u64.ToString(CultureInfo.InvariantCulture),
            long i64 => i64.ToString(CultureInfo.InvariantCulture),

            float f32 => f32.ToString(CultureInfo.InvariantCulture),
            double f64 => f64.ToString(CultureInfo.InvariantCulture),
            decimal d128 => d128.ToString(CultureInfo.InvariantCulture),

            Enum e => e.ToString(),
            char c => c.ToString(CultureInfo.InvariantCulture),
            string s => s,
            _ => null
        };

        if (utf16 is not (null or []))
        {
            result = utf16;
            return true;
        }

        result = null;
        return false;
    }

    static bool TryGetBytes(string utf16, [NotNullWhen(true)] out byte[]? result)
    {
        try
        {
            result = UTF8.GetBytes(utf16);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }

    string AddByteLiteral(byte[] utf8, bool nullTerminate)
    {
        var literalName = Guid.NewGuid().ToString("N");
        var byteLiteral = new StringBuilder((utf8.Length * 4) + 32)
            .Append("byte[] _")
            .Append(literalName)
            .Append(" = [");

        byteLiteral.Append(ByteLookup[utf8[0]]);
        foreach (var b in utf8.AsSpan(1))
        {
            byteLiteral.Append(',');
            byteLiteral.Append(ByteLookup[b]);
        }
        if (nullTerminate) byteLiteral.Append(",0");
        byteLiteral.Append("]");

        _literalValues.Add(byteLiteral.ToString());
        return literalName;
    }

    sealed class LiteralComparer : IEqualityComparer<object>
    {
        public new bool Equals(object x, object y)
        {
            return x switch
            {
                float f32 => y is float f32y && (Unsafe.As<float, int>(ref f32) == Unsafe.As<float, int>(ref f32y)),
                double f64 => y is double f64y && (Unsafe.As<double, long>(ref f64) == Unsafe.As<double, long>(ref f64y)),
                string s => y is string sy && s.Equals(sy, StringComparison.Ordinal),
                _ => x.Equals(y)
            };
        }

        public int GetHashCode(object obj)
        {
            return obj.GetHashCode();
        }
    }
}
