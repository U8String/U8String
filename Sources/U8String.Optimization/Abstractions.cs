using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace U8.Tools.Generators;

interface IOptimizationScope
{
    string Name { get; }
    IEnumerable<string> Imports { get; }
    IEnumerable<string> Fields { get; }
    IEnumerable<Interceptor> Interceptors { get; }

    bool ProcessCallsite(
        SemanticModel model,
        IMethodSymbol method,
        InvocationExpressionSyntax invocation);
}

record Interceptor(
    IMethodSymbol Method,
    string? InstanceArg,
    string[] CustomAttrs,
    List<Callsite> Callsites,
    string Body);

readonly struct Callsite
{
    public readonly string Path;
    public readonly int Line;
    public readonly int Character;

    public Callsite(
        IMethodSymbol method,
        InvocationExpressionSyntax invocation)
    {
        var lineSpan = invocation.GetLocation().GetLineSpan();
        var position = lineSpan.StartLinePosition;
        var offset = invocation.Expression.Span.Length - method.Name.Length;

        Path = lineSpan.Path;
        Line = position.Line + 1;
        Character = position.Character + offset + 1;
    }
}