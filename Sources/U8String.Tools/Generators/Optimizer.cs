using Microsoft.CodeAnalysis;

namespace U8.Tools.Generators;

// TODO: At some point, this will need some form of AST to allow
// optimization stages feed into each other before emitting the final
// code. This is also a requirement to eagerly detect and resolve
// interception conflicts which are bound to happen given how that
// the entirety of U8String is metric tons of overloads upon overloads.

// Stages:
// - FoldValidation | FoldConversion
// - Unroll(Comparison/Conversion/Copy)
// - SpecializeDispatch
// - DetectImports
// - EmitDependencies
// - EmitInterceptors

// State:
// - EmitDependencies
// - InterceptedLocations
//   - TrackedArguments
// - ConvertedLiterals
interface IOptimizationPhase
{
    bool IsEligible(IMethodSymbol method);
}

// [Generator]
class Optimizer // : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        throw new NotImplementedException();
    }

    public void Initialize(GeneratorInitializationContext context)
    {
        throw new NotImplementedException();
    }
}
