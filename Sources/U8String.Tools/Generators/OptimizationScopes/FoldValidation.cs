using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace U8.Tools.Generators.OptimizationScopes;

// Method types to fold validation for:
// U8String:
// - Concat
// - Join
// - Remove
// - Replace
// - ReplaceLineEndings
// - Strip(Prefix/Suffix)
// - Split, Split(First/Last)
// - Trim, Trim(Start/End)
// U8Builder:
// - Append
// - AppendLine
// - AppendLiteral
// InterpolatedU8StringHandler:
// - AppendLiteral
// - AppendFormatted

sealed class FoldValidation : IOptimizationScope
{
    readonly List<Interceptor> _interceptors = [];

    public string Name => "SkippedValidation";

    public IEnumerable<string> Imports =>
    [
        "System", "System.Runtime.CompilerServices",
        "U8", "U8.InteropServices", "U8.Primitives"
    ];

    public IEnumerable<string> Fields => [];

    public IEnumerable<Interceptor> Interceptors => _interceptors;

    public bool ProcessCallsite(
        SemanticModel model,
        IMethodSymbol method,
        InvocationExpressionSyntax invocation)
    {
        if (!IsSupportedMethod(method))
        {
            return false;
        }

        if (invocation.ArgumentList.Arguments is not { Count: 1 or 2 } args ||
            !args.All(arg => arg.Expression.IsKind(SyntaxKind.Utf8StringLiteralExpression)))
        {
            return false;
        }

        _interceptors.Add(new(
            ReturnType: method.ReturnType.Name,
            InstanceArg: "in this U8String source",
            Args: [..Enumerable.Repeat("ReadOnlySpan<byte>", args.Count)],
            GenericArgs: [],
            CustomAttrs: [Constants.AggressiveInlining],
            Callsites: [new Callsite(method, invocation)],
            Body: $"return U8Unchecked.{method.Name}(source, {string.Join(", ", Extensions.ArgRange(args.Count))});"));
        return true;
    }

    static bool IsSupportedMethod(IMethodSymbol method)
    {
        var methodName = method.Name;
        var containingType = method.ContainingType.Name;

        return (containingType, methodName) switch
        {
            ("U8String", "Remove") or
            ("U8String", "Replace") or
            ("U8String", "Split") or
            ("U8String", "SplitFirst") or
            ("U8String", "SplitLast") or
            ("U8String", "Strip") or
            ("U8String", "StripPrefix") or
            ("U8String", "StripSuffix") => true,

            _ => false
        };
    }
}