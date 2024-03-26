using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace U8.Optimization;

// Methods to specialize dispatch for:
// - Concat
// - Join
// - Split(...).To(Array/List/Slices)
// - SplitFirst/Last
// - SplitN (clarify name later)

sealed partial class SpecializeDispatch : IOptimizationScope
{
    readonly List<Interceptor> _interceptors = [];

    public string Name => "Dispatch";

    public IEnumerable<string> Imports =>
    [
        "System", "System.Runtime.CompilerServices",
        "U8", "U8.CompilerServices", "U8.Primitives"
    ];

    public IEnumerable<string> Fields => [];

    public IEnumerable<Interceptor> Interceptors => _interceptors;

    public bool ProcessCallsite(
        SemanticModel model,
        IMethodSymbol method,
        InvocationExpressionSyntax invocation)
    {
        var target = DispatchTarget.Resolve(method);
        if (target is null)
        {
            return false;
        }

        var body = target.EmitBody(model, invocation);
        if (body is null)
        {
            return false;
        }

        _interceptors.Add(new Interceptor(
            Method: target.Method,
            InstanceArg: target.InstanceArg,
            CustomAttrs: Constants.AggressiveInlining,
            Callsites: [Callsite.FromRegularInvocation(method, invocation)],
            Body: body));
        return true;
    }
}