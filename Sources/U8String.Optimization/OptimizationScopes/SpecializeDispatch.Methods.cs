using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace U8.Tools.Generators;

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
                "SplitFirst" => new SplitFirst(method),
                "SplitLast" => new SplitLast(method),
                _ => null
            };
        }

        public sealed class Concat(IMethodSymbol method) : DispatchTarget
        {
            public override IMethodSymbol Method => method;
            public override string? InstanceArg => null;

            public override string? EmitBody(SemanticModel model, InvocationExpressionSyntax invocation)
            {
                throw new NotImplementedException();
            }
        }

        public sealed class Join(IMethodSymbol method) : DispatchTarget
        {
            public override IMethodSymbol Method => method;
            public override string? InstanceArg => "in this IEnumerable<U8String>";

            public override string? EmitBody(SemanticModel model, InvocationExpressionSyntax invocation)
            {
                throw new NotImplementedException();
            }
        }

        public sealed class Remove(IMethodSymbol method) : DispatchTarget
        {
            public override IMethodSymbol Method => method;
            public override string? InstanceArg => "in this U8String";

            public override string? EmitBody(SemanticModel model, InvocationExpressionSyntax invocation)
            {
                throw new NotImplementedException();
            }
        }

        public sealed class Replace(IMethodSymbol method) : DispatchTarget
        {
            public override IMethodSymbol Method => method;
            public override string? InstanceArg => "in this U8String";

            public override string? EmitBody(SemanticModel model, InvocationExpressionSyntax invocation)
            {
                throw new NotImplementedException();
            }
        }

        public class SplitFirst(IMethodSymbol method) : DispatchTarget
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

                if (constantValue is byte b && b <= 0x7F)
                {
                    return "return source.SplitFirst((byte)arg0);";
                }

                if (constantValue is char c && !char.IsSurrogate(c))
                {
                    return c <= 0x7F
                        ? "return source.SplitFirst((byte)arg0);"
                        : $"return U8Unchecked.SplitFirst(source, \"{c}\"u8);";
                }

                return null;
            }
        }

        public class SplitLast(IMethodSymbol method) : DispatchTarget
        {
            public override IMethodSymbol Method => method;
            public override string? InstanceArg => "in this U8String";

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

                if (constantValue is byte b && b <= 0x7F)
                {
                    return "return source.SplitLast((byte)arg0);";
                }

                if (constantValue is char c && !char.IsSurrogate(c))
                {
                    return c <= 0x7F
                        ? "return source.SplitLast((byte)arg0);"
                        : $"return U8Unchecked.SplitLast(source, \"{c}\"u8);";
                }

                return null;
            }
        }
    }
}