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

        public static DispatchTarget? Match(IMethodSymbol method)
        {
            return method.Name switch
            {
                "Concat" => new Concat(method),
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
            public override string? InstanceArg => "in this U8String";

            public override string? EmitBody(SemanticModel model, InvocationExpressionSyntax invocation)
            {
                throw new NotImplementedException();
            }
        }

        public class SplitLast(IMethodSymbol method) : DispatchTarget
        {
            public override IMethodSymbol Method => method;
            public override string? InstanceArg => "in this U8String";

            public override string? EmitBody(SemanticModel model, InvocationExpressionSyntax invocation)
            {
                throw new NotImplementedException();
            }
        }
    }
}