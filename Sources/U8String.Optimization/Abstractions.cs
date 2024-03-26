using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace U8.Optimization;

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

    public static Callsite FromExtensionInvocation(IInvocationOperation invocation)
    {
        return new Callsite(invocation);
    }

    public static Callsite FromRegularInvocation(
        IMethodSymbol method,
        InvocationExpressionSyntax invocation)
    {
        return new Callsite(method, invocation);
    }

    Callsite(IInvocationOperation invocation)
    {
        var memberAccessorExpression = (MemberAccessExpressionSyntax)((InvocationExpressionSyntax)invocation.Syntax).Expression;
        var invocationNameSpan = memberAccessorExpression.Name.Span;
        var lineSpan = invocation.Syntax.SyntaxTree.GetLineSpan(invocationNameSpan);
        var filePath = invocation.Syntax.SyntaxTree.FilePath;

        Path = filePath;
        Line = lineSpan.StartLinePosition.Line + 1;
        Character = lineSpan.StartLinePosition.Character + 1;
    }

    Callsite(
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