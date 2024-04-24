using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace U8.Optimization.OptimizationScopes;

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
    sealed record Utf8LiteralExpression(string Value)
    {
        public bool Equals(Utf8LiteralExpression? obj)
        {
            return obj != null && Value.Equals(obj.Value, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }
    }

    readonly record struct LiteralDeclaration(string TypeName, object Value, bool IsExtensionMethod)
    {
        public bool Equals(LiteralDeclaration obj)
        {
            if (obj.TypeName is null || obj.Value is null)
            {
                return false;
            }

            if (IsExtensionMethod != obj.IsExtensionMethod || !TypeName.Equals(obj.TypeName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return Value is string xs && obj.Value is string ys
                ? xs.Equals(ys, StringComparison.Ordinal) : Value.Equals(obj.Value);
        }

        public override int GetHashCode()
        {
            if (TypeName is null || Value is null)
            {
                return 0;
            }

            var hashcode = 1430287;
            var valueHashCode = Value is string s ? StringComparer.Ordinal.GetHashCode(s) : Value.GetHashCode();

            hashcode = hashcode * 7302013 ^ StringComparer.OrdinalIgnoreCase.GetHashCode(TypeName);
            hashcode = hashcode * 7302013 ^ valueHashCode;
            hashcode = hashcode * 7302013 ^ IsExtensionMethod.GetHashCode();
            return hashcode;
        }
    }

    static readonly UTF8Encoding UTF8 = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    static readonly string[] ByteLookup = Enumerable
        .Range(0, 256)
        .Select(i => i.ToString(CultureInfo.InvariantCulture))
        .ToArray();

    readonly List<string> _literalValues = [];
    readonly Dictionary<LiteralDeclaration, Interceptor> _literalMap = [];

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
            ("U8String", "FromAscii") => true,
            ("U8StringExtensions", "ToU8String") => true,
            ("U8EnumExtensions", "ToU8String") => true,
            ("Syntax", "u8") => true,

            _ => false
        };
    }

    public bool ProcessCallsite(
        SemanticModel model,
        IMethodSymbol method,
        InvocationExpressionSyntax invocation)
    {
        if (!IsSupportedMethod(method))
        {
            return false;
        }

        bool isExtensionMethod;
        ExpressionSyntax? expression;

        // Handle u8(...), U8String.Create(...), etc.
        var invocationOperation = (IInvocationOperation)model.GetOperation(invocation)!;
        if (invocation.ArgumentList.Arguments is [var argument]
            && method.Name is not "ToU8String") // Is there a cleaner way?
        {
            isExtensionMethod = false;
            expression = argument.Expression;
        }
        // Handle instance.ToU8String()
        else if (invocationOperation.Arguments is [var instanceArgument]
            && instanceArgument.Value is IOperation instanceOperation
            && instanceOperation.Syntax is ExpressionSyntax literalExpression)
        {
            isExtensionMethod = true;
            expression = literalExpression;
        }
        else
        {
            return false;
        }

        var constant = model.GetConstantValue(expression);
        if (constant is not { HasValue: true, Value: object constantValue })
        {
            if (expression.IsKind(SyntaxKind.Utf8StringLiteralExpression)
                && model.GetOperation(expression) is IUtf8StringOperation operation
                && operation.Value is not (null or []))
            {
                constantValue = new Utf8LiteralExpression(operation.Value);
            }
            else return false;
        }

        var type = model.GetTypeInfo(expression).Type;
        if (type is INamedTypeSymbol { TypeKind: TypeKind.Enum } symbol)
        {
            var isFlags = symbol
                .GetAttributes()
                .Any(attr => attr.AttributeClass is
                {
                    Name: "FlagsAttribute",
                    ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true }
                });

            if (isFlags)
            {
                // The types which can be Convert.ToInt32'd
                if (constantValue is not (byte or ushort or int))
                {
                    return false;
                }

                constantValue = FormatEnumFlags(symbol, constantValue);
            }
            else
            {
                // Resolve enum member name from the constant value.
                // If it's not resolved, it will stay numeric, matching runtime behavior.
                foreach (var member in symbol.GetMembers())
                {
                    if (member is IFieldSymbol field && (field.ConstantValue?.Equals(constantValue) ?? false))
                    {
                        constantValue = member.Name;
                        break;
                    }
                }
            }
        }

        var callsite = isExtensionMethod
            ? Callsite.FromExtensionInvocation(invocationOperation)
            : Callsite.FromRegularInvocation(method, invocation);
        var literal = new LiteralDeclaration(type!.ToDisplayString(), constantValue, isExtensionMethod);

        if (_literalMap.TryGetValue(literal, out var interceptor))
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

        var instanceArg = isExtensionMethod ? $"this {method.ReceiverType} source" : null;
        var literalName = AddByteLiteral(utf8, utf16[^1] != 0);

        _literalMap[literal] = new(
            Method: method,
            InstanceArg: instanceArg,
            CustomAttrs: Constants.AggressiveInlining,
            Callsites: [callsite],
            Body: $"return U8Marshal.CreateUnsafe(_{literalName}, 0, {utf8.Length});");

        return true;
    }

    static object FormatEnumFlags(INamedTypeSymbol symbol, object value)
    {
        Debug.Assert(value is byte or ushort or int);

        var flags = new StringBuilder();
        var underlying = Convert.ToInt32(value);

        foreach (var member in symbol.GetMembers())
        {
            if (member is IFieldSymbol field && field.HasConstantValue)
            {
                var memberValue = Convert.ToInt32(field.ConstantValue!);
                if (memberValue is 0) continue;
                if ((underlying & memberValue) == memberValue)
                {
                    flags.Append(flags.Length == 0 ? field.Name : $", {field.Name}");
                    underlying &= ~memberValue;
                }
            }
        }

        return underlying is 0 ? flags.ToString() : value;
    }

    static bool TryGetString(object? value, [NotNullWhen(true)] out string? result)
    {
        var invariantCulture = CultureInfo.InvariantCulture;
        var utf16 = value switch
        {
            byte u8 => u8.ToString(invariantCulture),
            sbyte i8 => i8.ToString(invariantCulture),
            ushort u16 => u16.ToString(invariantCulture),
            short i16 => i16.ToString(invariantCulture),
            uint u32 => u32.ToString(invariantCulture),
            int i32 => i32.ToString(invariantCulture),
            ulong u64 => u64.ToString(invariantCulture),
            long i64 => i64.ToString(invariantCulture),

            decimal d128 => d128.ToString(invariantCulture),

            Enum e => e.ToString(),
            char c => c.ToString(invariantCulture),
            string s => s,
            Utf8LiteralExpression u8 => u8.Value,
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
        var literalLength = nullTerminate ? utf8.Length + 1 : utf8.Length;
        var byteLiteral = new StringBuilder((utf8.Length * 4) + 32)
            .Append("byte[] _")
            .Append(literalName)
            .Append($" = new byte[{literalLength}] {{");

        byteLiteral.Append(ByteLookup[utf8[0]]);
        foreach (var b in utf8.AsSpan(1))
        {
            byteLiteral.Append(',');
            byteLiteral.Append(ByteLookup[b]);
        }
        if (nullTerminate) byteLiteral.Append(",0");
        byteLiteral.Append("}");

        _literalValues.Add(byteLiteral.ToString());
        return literalName;
    }
}
