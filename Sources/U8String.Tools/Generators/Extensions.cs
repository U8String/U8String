using System.Diagnostics.CodeAnalysis;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace U8.Tools.Generators;

static class Extensions
{
    internal static string? NormalizePath(this Compilation compilation, string path)
    {
        return compilation.Options.SourceReferenceResolver?.NormalizePath(path, baseFilePath: null);
    }

    internal static bool TryGetInvocation(
        this SemanticModel semanticModel,
        SyntaxNode node,
        [NotNullWhen(true)] out InvocationExpressionSyntax? invocation,
        [NotNullWhen(true)] out IMethodSymbol? methodSymbol)
    {
        if (node is InvocationExpressionSyntax i
            && semanticModel.GetSymbolInfo(i).Symbol is IMethodSymbol m
            && m.ContainingAssembly.Name is "U8String")
        {
            invocation = i;
            methodSymbol = m;
            return true;
        }

        invocation = null;
        methodSymbol = null;
        return false;
    }

    internal static string[] ArgRange(int count)
    {
        return Enumerable.Range(0, count).Select(i => $"arg{i}").ToArray();
    }
}
