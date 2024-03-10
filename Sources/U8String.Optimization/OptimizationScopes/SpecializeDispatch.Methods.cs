using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace U8.Optimization;

// TODO: Expand method implementations within select dispatch targets
sealed partial class SpecializeDispatch
{
    abstract class DispatchTarget
    {
        public abstract IMethodSymbol Method { get; }
        public abstract string? InstanceArg { get; }

        public abstract string? EmitBody(
            SemanticModel model,
            InvocationExpressionSyntax invocation);

        public static DispatchTarget? Resolve(IMethodSymbol method)
        {
            if (method.ContainingType.Name != "U8String")
            {
                return null;
            }

            return method.Name switch
            {
                // "ReplaceLineEndings" => new ReplaceLineEndings(method),
                "Join" => new Join(method),

                "SplitFirst" => new SplitFirst(method),
                "SplitLast" => new SplitLast(method),

                "StartsWith" => new StartsWith(method),
                "EndsWith" => new EndsWith(method),

                "Strip" => new Strip(method),
                "StripPrefix" => new StripPrefix(method),
                "StripSuffix" => new StripSuffix(method),

                _ => null
            };
        }

        sealed class Concat(IMethodSymbol method) : DispatchTarget
        {
            public override IMethodSymbol Method => method;
            public override string? InstanceArg => null;

            public override string? EmitBody(SemanticModel model, InvocationExpressionSyntax invocation)
            {
                throw new NotImplementedException();
            }
        }

        sealed class Join(IMethodSymbol method) : DispatchTarget
        {
            public override IMethodSymbol Method => method;
            public override string? InstanceArg => null;

            public override string? EmitBody(SemanticModel model, InvocationExpressionSyntax invocation)
            {
                var args = invocation.ArgumentList.Arguments;
                // Only simple forms are supported for now.
                // TODO: support IUtf8SpanFormattable and known struct enumeration overloads
                if (args is not [var separator, var values])
                {
                    return null;
                }

                string? separatorText = null, valuesText = null;

                // Resolve separator argument text
                var separatorExpr = separator.Expression;
                var constant = model.GetConstantValue(separatorExpr);
                if (constant is { HasValue: true, Value: object value })
                {
                    separatorText = value switch
                    {
                        byte b when b <= 0x7F => $"{b}",
                        char c when !char.IsSurrogate(c) =>
                            c <= 0x7F ? $"(byte)'{c}'" : $"\"{c}\"u8",
                        _ => null
                    };
                }
                else if (
                    model.GetTypeInfo(separatorExpr).ConvertedType.IsReadOnlyByteSpan() && (
                    separatorExpr.IsKind(SyntaxKind.Utf8StringLiteralExpression) ||
                    model.GetOperation(separatorExpr)?.Type.IsU8String() is true))
                {
                    separatorText = "arg0";
                }

                // Bail out early if separator is not eligible
                if (separatorText is null)
                {
                    return null;
                }

                var valuesType = model.GetTypeInfo(values.Expression).ConvertedType;
                if (valuesType.IsArrayOf(Extensions.IsU8String) || (
                    valuesType is INamedTypeSymbol
                    {
                        Name: "ReadOnlySpan" or "IEnumerable",
                        TypeArguments: [var typeArg]
                    } && typeArg.IsU8String()))
                {
                    valuesText = "arg1";
                }

                if (valuesText is null)
                {
                    return null;
                }

                return $"return U8Unchecked.Join({separatorText}, {valuesText});";
            }
        }

        sealed class Remove(IMethodSymbol method) : DispatchTarget
        {
            public override IMethodSymbol Method => method;
            public override string? InstanceArg => "in this U8String source";

            public override string? EmitBody(SemanticModel model, InvocationExpressionSyntax invocation)
            {
                throw new NotImplementedException();
            }
        }

        sealed class Replace(IMethodSymbol method) : DispatchTarget
        {
            public override IMethodSymbol Method => method;
            public override string? InstanceArg => "in this U8String source";

            public override string? EmitBody(SemanticModel model, InvocationExpressionSyntax invocation)
            {
                throw new NotImplementedException();
            }
        }

        sealed class ReplaceLineEndings(IMethodSymbol method) : DispatchTarget
        {
            public override IMethodSymbol Method => method;
            public override string? InstanceArg => "in this U8String source";

            public override string? EmitBody(SemanticModel model, InvocationExpressionSyntax invocation)
            {
                // Argument-less overloads needs no special handling
                if (invocation.ArgumentList.Arguments is not [var argument])
                {
                    return null;
                }

                throw new NotImplementedException();
            }
        }

        sealed class SplitFirst(IMethodSymbol method) : DispatchTarget
        {
            public override IMethodSymbol Method => method;
            public override string? InstanceArg => "in this U8String source";

            public override string? EmitBody(SemanticModel model, InvocationExpressionSyntax invocation)
            {
                // This assumes that FoldValidation has already ran - brittle and "not good" but it's a start
                // Only non-comparer overloads are supported for now
                if (invocation.ArgumentList.Arguments is not [var argument])
                {
                    return null;
                }

                var constant = model.GetConstantValue(argument.Expression);
                if (constant is not { HasValue: true, Value: object constantValue })
                {
                    return null;
                }

                if (constantValue is char c && !char.IsSurrogate(c))
                {
                    return c <= 0x7F
                        ? $"return source.SplitFirst((byte)'{c}');"
                        : $"return U8Unchecked.SplitFirst(source, \"{c}\"u8);";
                }

                return null;
            }
        }

        sealed class SplitLast(IMethodSymbol method) : DispatchTarget
        {
            public override IMethodSymbol Method => method;
            public override string? InstanceArg => "in this U8String source";

            public override string? EmitBody(SemanticModel model, InvocationExpressionSyntax invocation)
            {
                if (invocation.ArgumentList.Arguments is not [var argument])
                {
                    return null;
                }

                var constant = model.GetConstantValue(argument.Expression);
                if (constant is not { HasValue: true, Value: object constantValue })
                {
                    return null;
                }

                if (constantValue is char c && !char.IsSurrogate(c))
                {
                    return c <= 0x7F
                        ? $"return source.SplitLast((byte)'{c}');"
                        : $"return U8Unchecked.SplitLast(source, \"{c}\"u8);";
                }

                return null;
            }
        }

        sealed class StartsWith(IMethodSymbol method) : DispatchTarget
        {
            public override IMethodSymbol Method => method;
            public override string? InstanceArg => "in this U8String source";

            public override string? EmitBody(SemanticModel model, InvocationExpressionSyntax invocation)
            {
                // TODO: Support comparer variants
                if (invocation.ArgumentList.Arguments is not [var argument])
                {
                    return null;
                }

                var constant = model.GetConstantValue(argument.Expression);
                if (constant is not { HasValue: true, Value: object constantValue })
                {
                    return null;
                }

                if (constantValue is char c && !char.IsSurrogate(c))
                {
                    return c <= 0x7F
                        ? $"return source.StartsWith((byte)'{c}');"
                        : $"return source.StartsWith(\"{c}\"u8);";
                }

                return null;
            }
        }

        sealed class EndsWith(IMethodSymbol method) : DispatchTarget
        {
            public override IMethodSymbol Method => method;
            public override string? InstanceArg => "in this U8String source";

            public override string? EmitBody(SemanticModel model, InvocationExpressionSyntax invocation)
            {
                if (invocation.ArgumentList.Arguments is not [var argument])
                {
                    return null;
                }

                var constant = model.GetConstantValue(argument.Expression); 
                if (constant is not { HasValue: true, Value: object constantValue })
                {
                    return null;
                }

                if (constantValue is char c && !char.IsSurrogate(c))
                {
                    return c <= 0x7F
                        ? $"return source.EndsWith((byte)'{c}');"
                        : $"return source.EndsWith(\"{c}\"u8);";
                }

                return null;
            }
        }

        sealed class Strip(IMethodSymbol method) : DispatchTarget
        {
            public override IMethodSymbol Method => method;
            public override string? InstanceArg => "in this U8String source";

            public override string? EmitBody(SemanticModel model, InvocationExpressionSyntax invocation)
            {
                if (invocation.ArgumentList.Arguments is not { Count: 1 or 2 } args)
                {
                    return null;
                }

                if (args is [var singleArg])
                {
                    var constant = model.GetConstantValue(singleArg.Expression);
                    if (constant is not { HasValue: true, Value: object value })
                    {
                        return null;
                    }

                    if (value is char c && !char.IsSurrogate(c))
                    {
                        return c <= 0x7F
                            ? "return source.Strip((byte)arg0);"
                            : $"return U8Unchecked.Strip(source, \"{c}\"u8);";
                    }
                }
                else
                {
                    var prefix = model.GetConstantValue(args[0].Expression);
                    var suffix = model.GetConstantValue(args[1].Expression);
                    if (prefix is not { HasValue: true, Value: object prefixValue } ||
                        suffix is not { HasValue: true, Value: object suffixValue })
                    {
                        return null;
                    }

                    if (prefixValue is char pc && !char.IsSurrogate(pc) &&
                        suffixValue is char sc && !char.IsSurrogate(sc))
                    {
                        return pc <= 0x7F && sc <= 0x7F
                            ? "return source.Strip((byte)arg0, (byte)arg1);"
                            : $"return U8Unchecked.Strip(source, \"{pc}\"u8, \"{sc}\"u8);";
                    }
                }

                return null;
            }
        }

        sealed class StripPrefix(IMethodSymbol method) : DispatchTarget
        {
            public override IMethodSymbol Method => method;
            public override string? InstanceArg => "in this U8String source";

            public override string? EmitBody(SemanticModel model, InvocationExpressionSyntax invocation)
            {
                if (invocation.ArgumentList.Arguments is not [var argument])
                {
                    return null;
                }

                var constant = model.GetConstantValue(argument.Expression);
                if (constant is not { HasValue: true, Value: object value })
                {
                    return null;
                }

                if (value is char c && !char.IsSurrogate(c))
                {
                    return c <= 0x7F
                        ? "return source.StripPrefix((byte)arg0);"
                        : $"return U8Unchecked.StripPrefix(source, \"{c}\"u8);";
                }

                return null;
            }
        }

        sealed class StripSuffix(IMethodSymbol method) : DispatchTarget
        {
            public override IMethodSymbol Method => method;
            public override string? InstanceArg => "in this U8String source";

            public override string? EmitBody(SemanticModel model, InvocationExpressionSyntax invocation)
            {
                if (invocation.ArgumentList.Arguments is not [var argument])
                {
                    return null;
                }

                var constant = model.GetConstantValue(argument.Expression);
                if (constant is not { HasValue: true, Value: object value })
                {
                    return null;
                }

                if (value is char c && !char.IsSurrogate(c))
                {
                    return c <= 0x7F
                        ? "return source.StripSuffix((byte)arg0);"
                        : $"return U8Unchecked.StripSuffix(source, \"{c}\"u8);";
                }

                return null;
            }
        }
    }
}