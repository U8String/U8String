using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace U8.Optimization.OptimizationScopes;

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
        "U8", "U8.CompilerServices", "U8.Primitives"
    ];

    public IEnumerable<string> Fields => [];

    public IEnumerable<Interceptor> Interceptors => _interceptors;

    static bool IsSupportedMethod(IMethodSymbol method)
    {
        var methodName = method.Name;
        var containingType = method.ContainingType.Name;

        return (containingType, methodName) switch
        {
            // TODO: Handle these in specialize dispatch or fold conversion?
            // Alternatively, flatten all optimization phases into a flat pass where each
            // method has individual handling (similar to what is currently scaffolded in SpecializeDispatch)
            // ("U8String", "Concat") or
            // ("U8String", "Join") or
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

    public bool ProcessCallsite(
        SemanticModel model,
        IMethodSymbol method,
        InvocationExpressionSyntax invocation)
    {
        if (!IsSupportedMethod(method))
        {
            return false;
        }

        var (validated, unknown) = invocation
            .ArgumentList
            .Arguments
            .CountUtf8Arguments(model);

        if (validated is 0 || unknown > 0)
        {
            return false;
        }

        _interceptors.Add(new(
            Method: method,
            InstanceArg: "in this U8String source",
            CustomAttrs: Constants.AggressiveInlining,
            Callsites: [Callsite.FromRegularInvocation(method, invocation)],
            Body: $"return U8Unchecked.{method.Name}(source, {string.Join(", ", Extensions.ArgRange(invocation.ArgumentList.Arguments.Count))});"));
        return true;
    }
}
