using Microsoft.CodeAnalysis;

namespace U8.Tools.Generators;

static class Extensions
{
    internal static string? NormalizePath(this Compilation compilation, string path)
    {
        return compilation.Options.SourceReferenceResolver?.NormalizePath(path, baseFilePath: null);
    }
}
