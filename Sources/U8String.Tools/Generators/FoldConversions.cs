using System.Globalization;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace U8.Tools.Generators;

// TODO:
// U8Builder:
// - Append
// - AppendLine
// - AppendLiteral
// InterpolatedU8StringHandler: (is this even possible?)
// - AppendLiteral
// - AppendFormatted
[Generator]
public class FoldConversions : ISourceGenerator
{
    record ConversionLocation(string Path, int Line, int Character);

    readonly static UTF8Encoding UTF8 = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    readonly static string[] ByteValues = Enumerable
        .Range(0, 256)
        .Select(i => i.ToString(CultureInfo.InvariantCulture))
        .ToArray();

    public void Initialize(GeneratorInitializationContext context) { }

    // TODO: Port to F# if possible, or not
    // TODO: Profile and optimize, it really kills build times on large string literals
    public void Execute(GeneratorExecutionContext context)
    {
        var compilation = context.Compilation;
        var uniqueLiterals = new Dictionary<object, List<ConversionLocation>>();

        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var semanticModel = compilation
                .GetSemanticModel(syntaxTree);

            var invocations = syntaxTree
                .GetRoot()
                .DescendantNodes()
                .OfType<InvocationExpressionSyntax>();

            foreach (var (literal, conversionLocation) in EnumerateConversions(semanticModel, invocations))
            {
                var literalLocations = uniqueLiterals.TryGetValue(literal, out var list)
                    ? list : uniqueLiterals[literal] = [];

                // TODO: This looks embarrassing, is there a better way?
                literalLocations.Add(conversionLocation);
            }
        }

        var source = new StringBuilder().Append("""
            // <auto-generated />
            using global::System;
            using global::U8;
            using global::U8.InteropServices;

            #pragma warning disable CS9113
            namespace System.Runtime.CompilerServices
            {
                [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
                file class InterceptsLocationAttribute(string filePath, int line, int character) : Attribute;
            }
            #pragma warning restore CS9113

            namespace U8.Generated
            {
                file static class U8Literals
                {
            """);

        foreach (var (literalValue, literalLocations) in uniqueLiterals)
        {
            source.AppendLine();

            // TODO: Figure out a way to reliably not conflict with
            // culture-specific formatting while retaining the ability
            // to actually fold these.
            var utf16 = literalValue switch
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

                char c => c.ToString(CultureInfo.InvariantCulture),
                string s => s,
                _ => null
            };

            if (utf16 is null or []) continue;

            var nullTerminate = utf16[^1] != 0;

            Span<byte> bytes;
            try
            {
                bytes = UTF8.GetBytes(utf16);
            }
            catch
            {
                continue;
            }

            var constantType = literalValue.GetType().Name;
            var prefix = literalValue is not (string or bool or byte)
                ? $"<{constantType}>" : string.Empty;

            var guid = Guid.NewGuid().ToString("N");
            var literalName = $"_{guid}";
            var literalAccessor = $"__{guid}";
            var byteLiteral = new StringBuilder(bytes.Length * 4);

            byteLiteral.Append(ByteValues[bytes[0]]);
            foreach (var b in bytes[1..])
            {
                byteLiteral.Append(',');
                byteLiteral.Append(ByteValues[b]);
            }
            if (nullTerminate) byteLiteral.Append(",0");

            source.AppendLine($$"""
                    static readonly byte[] {{literalName}} = new byte[] {{{byteLiteral}}};
            """);
            
            foreach (var location in literalLocations)
            {
                var normalizedPath = compilation.NormalizePath(location.Path);
                if (normalizedPath is null or []) continue;

                source.AppendLine($$"""
                        [System.Runtime.CompilerServices.InterceptsLocation(@"{{normalizedPath}}", line: {{location.Line}}, character: {{location.Character}})]
                """);
            }
            
            source.AppendLine($$"""
                    public static U8String {{literalAccessor}}{{prefix}}({{constantType}} _) => U8Marshal.CreateUnsafe({{literalName}}, 0, {{bytes.Length}});
            """);
        }

        source.Append("""
                }
            }
            """);

        context.AddSource("U8Literals.g.cs", source.ToString());
    }

    static IEnumerable<(object Value, ConversionLocation)> EnumerateConversions(
        SemanticModel model, IEnumerable<InvocationExpressionSyntax> invocations)
    {
        foreach (var invocation in invocations)
        {
            var symbolInfo = model.GetSymbolInfo(invocation);
            if (symbolInfo.Symbol is IMethodSymbol methodSymbol
                && IsConversionCall(methodSymbol)
                && invocation.ArgumentList.Arguments is [var argument])
            {
                var constant = model.GetConstantValue(argument.Expression);
                if (constant is { HasValue: true, Value: object constantValue })
                {
                    var lineSpan = invocation.GetLocation().GetLineSpan();
                    if (!lineSpan.IsValid) continue;

                    var path = lineSpan.Path;
                    var position = lineSpan.StartLinePosition;
                    var line = position.Line + 1;
                    var offset = invocation.Expression.Span.Length - methodSymbol.Name.Length;
                    var character = position.Character + offset + 1;

                    yield return (constantValue, new(path, line, character));
                }
            }
        }
    }

    static bool IsConversionCall(IMethodSymbol methodSymbol)
    {
        if (methodSymbol.ContainingAssembly.Name != "U8String")
        {
            return false;
        }

        var containingType = methodSymbol.ContainingType.Name;
        var methodName = methodSymbol.Name;

        return (containingType, methodName) switch
        {
            ("U8String", "Create") or
            ("U8String", "CreateLossy") or
            ("U8String", "CreateInterned") or
            ("U8String", "FromAscii") or
            ("U8String", "FromLiteral") => true,

            ("Syntax", "u8") => true,

            _ => false
        };
    }
}
