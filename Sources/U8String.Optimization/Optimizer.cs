using System.Collections.Immutable;
using System.Text;

using Microsoft.CodeAnalysis;

using U8.Optimization.OptimizationScopes;

namespace U8.Optimization;

// TODO: At some point, this will need some form of AST to allow
// optimization stages feed into each other before emitting the final
// code. This is also a requirement to eagerly detect and resolve
// interception conflicts which are bound to happen given how that
// the entirety of U8String is metric tons of overloads upon overloads.

[Generator]
sealed class Optimizer : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext _) { }

    public void Execute(GeneratorExecutionContext context)
    {
        var compilation = context.Compilation;
        var optimizations = new IOptimizationScope[]
        {
            new FoldConversions(),
            new FoldValidation(),
            new SpecializeDispatch()
        };

        foreach (var tree in compilation.SyntaxTrees)
        {
            var model = compilation
                .GetSemanticModel(tree);

            var nodes = tree
                .GetRoot()
                .DescendantNodes();

            foreach (var node in nodes)
            {
                if (model.TryGetInvocation(node, out var invocation, out var method))
                {
                    foreach (var optimization in optimizations)
                    {
                        if (optimization.ProcessCallsite(model, method, invocation))
                        {
                            break;
                        }
                    }
                }
            }
        }

        foreach (var scope in optimizations)
        {
            if (!scope.Interceptors.Any())
            {
                continue;
            }

            context.AddSource($"U8{scope.Name}.g.cs", EmitInterceptors(compilation, scope));
        }
    }

    string EmitInterceptors(Compilation compilation, IOptimizationScope scope)
    {
        var source = new StringBuilder("// <auto-generated />");

        source.AppendLine();
        foreach (var import in scope.Imports)
        {
            source.AppendLine($"using global::{import};");
        }

        source.AppendLine();
        source.AppendLine("""
            #pragma warning disable CS9113
            namespace System.Runtime.CompilerServices
            {
                [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
                file class InterceptsLocationAttribute(string filePath, int line, int character) : Attribute;
            }
            #pragma warning restore CS9113
            """);

        source.AppendLine();
        source.AppendLine($$"""
            namespace U8.Generated
            {
                file static class U8{{scope.Name}}
                {
            """);

        foreach (var field in scope.Fields)
        {
            source.AppendLine($"""
                    static readonly {field};
            """);
        }

        foreach (var interceptor in scope.Interceptors)
        {
            source.AppendLine();

            // Custom attributes (like AggressiveInlining)
            foreach (var attribute in interceptor.CustomAttrs)
            {
                source.AppendLine($"""
                        [{attribute}]
                """);
            }

            // Callsite interception specifiers
            foreach (var callsite in interceptor.Callsites)
            {
                var path = compilation.NormalizePath(callsite.Path);

                source.AppendLine($"""
                        [InterceptsLocation(@"{path}", {callsite.Line}, {callsite.Character})]
                """);
            }

            // Accessibility modifiers
            source.Append("        public static ");
            source.Append(interceptor.Method.ReturnType.Name);
            source.Append(' ');

            // Method name
            source.Append($"__{Guid.NewGuid():N}");

            // Generic arguments
            var genericArgs = interceptor.Method.TypeParameters;
            if (genericArgs is not [])
            {
                source.Append(FormatGenericArgs(genericArgs));
            }

            source.Append('(');
            // Instance type as a first argument (if any)
            if (interceptor.InstanceArg is not null)
            {
                source.Append(interceptor.InstanceArg);
            }

            // Arguments (if any)
            var arguments = FormatArgs(interceptor.Method);
            if (arguments is not "")
            {
                if (interceptor.InstanceArg != null)
                {
                    source.Append(", ");
                }

                source.Append(arguments);
            }

            // Method signature end
            source.AppendLine(")");
            // FIXME: this has ruined formatting for multi-line bodies
            source.AppendLine($$"""
                    {
                        {{interceptor.Body}}
                    }
            """);
        }

        source.Append("""
                }
            }
            """);

        return source.ToString();
    }

    static string FormatArgs(IMethodSymbol method)
    {
        static string FormatType(IParameterSymbol param) =>
            param.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        var args = method
            .Parameters
            .Select((param, i) => $"{FormatType(param)} arg{i}");

        return string.Join(", ", args);
    }

    static string FormatGenericArgs(ImmutableArray<ITypeParameterSymbol> genericArgs)
    {
        static string FormatType(ITypeParameterSymbol arg) =>
            arg.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        return $"<{string.Join(", ", genericArgs.Select(FormatType))}>";
    }
}
