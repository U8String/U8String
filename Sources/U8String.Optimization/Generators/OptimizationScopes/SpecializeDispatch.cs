using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace U8.Tools.Generators;

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
        "System", "System.Text",
        "U8", "U8.InteropServices"
    ];

    public IEnumerable<string> Fields => [];

    public IEnumerable<Interceptor> Interceptors => _interceptors;

    public bool ProcessCallsite(
        SemanticModel model,
        IMethodSymbol method,
        InvocationExpressionSyntax invocation)
    {
        throw new NotImplementedException();
    }
}