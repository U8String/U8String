using System.Diagnostics.CodeAnalysis;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace U8.Optimization;

static class Extensions
{
    internal static string? NormalizePath(this Compilation compilation, string path)
    {
        return compilation.Options.SourceReferenceResolver?.NormalizePath(path, baseFilePath: null);
    }

    public static bool IsU8String(this ITypeSymbol? type) => type is
    {
        Name: "U8String",
        ContainingAssembly.Name: "U8String",
        ContainingNamespace:
        {
            Name: "U8",
            ContainingNamespace.IsGlobalNamespace: true
        }
    };

    internal static bool IsArrayOf(this ITypeSymbol? type, Func<ITypeSymbol?, bool> predicate)
    {
        return type is IArrayTypeSymbol { ElementType: var elementType } && predicate(elementType);
    }

    internal static bool IsReadOnlySpanOf(this ITypeSymbol? type, Func<ITypeSymbol?, bool> predicate)
    {
        return type is INamedTypeSymbol namedSymbol
            && namedSymbol is
            {
                Name: "ReadOnlySpan",
                ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true }
            }
            && namedSymbol.TypeArguments is [var typeArgument]
            && predicate(typeArgument);
    }

    internal static bool IsByteArray(this ITypeSymbol? type)
    {
        return type is IArrayTypeSymbol
        {
            ElementType.SpecialType: SpecialType.System_Byte
        };
    }

    internal static bool IsReadOnlyByteSpan(this ITypeSymbol? type)
    {
        return type is INamedTypeSymbol
        {
            Name: "ReadOnlySpan",
            ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true },
            TypeArguments: [{ SpecialType: SpecialType.System_Byte }]
        };
    }

    internal static bool IsFromU8StringConversion(this IOperation? operation)
    {
        return operation is IConversionOperation
        {
            OperatorMethod:
            {
                Name: "op_Implicit" or "op_Explicit",
                ReceiverType.Name: "U8String",
                ContainingType.Name: "U8String",
                ContainingNamespace: { Name: "U8", ContainingNamespace.IsGlobalNamespace: true }
            }
        };
    }

    internal static (int Validated, int Unknown) CountUtf8Arguments(
        this SeparatedSyntaxList<ArgumentSyntax> arguments,
        SemanticModel model)
    {
        var validated = 0;
        var unknown = 0;
        foreach (var arg in arguments)
        {
            var expression = arg.Expression;
            if (model.GetTypeInfo(expression).ConvertedType.IsReadOnlyByteSpan())
            {
                if (expression.IsKind(SyntaxKind.Utf8StringLiteralExpression))
                {
                    validated++;
                    continue;
                }

                var operation = model.GetOperation(expression);
                if (operation?.Type.IsU8String() ?? false)
                {
                    validated++;
                    continue;
                }

                if ((operation as IArgumentOperation)?.Value.IsFromU8StringConversion() ?? false)
                {
                    validated++;
                    continue;
                }

                unknown++;
            }
        }

        return (validated, unknown);
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
