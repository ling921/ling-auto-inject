using Microsoft.CodeAnalysis;

namespace Ling.AutoInject.SourceGenerators.Helpers;

internal static class CompilationHelpers
{
    public static Version? FindReferenceAssemblyVersionByTypeMetadataName(this Compilation compilation, string typeMetadataName)
    {
        var typeSymbol = compilation.GetTypeByMetadataName(typeMetadataName);
        return typeSymbol?.ContainingAssembly.Identity.Version;
    }
}
