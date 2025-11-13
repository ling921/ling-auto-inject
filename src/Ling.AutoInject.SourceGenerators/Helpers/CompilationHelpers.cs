using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Ling.AutoInject.SourceGenerators.Helpers;

internal static class CompilationHelpers
{
    public static Version? FindReferenceAssemblyVersionByTypeMetadataName(this Compilation compilation, string typeMetadataName)
    {
        var typeSymbol = compilation.GetTypeByMetadataName(typeMetadataName);
        return typeSymbol?.ContainingAssembly.Identity.Version;
    }

    public static string? GetNamespace(this Compilation compilation, AnalyzerConfigOptionsProvider optionsProvider)
    {
        return optionsProvider.GlobalOptions.TryGetValue("build_property.TargetNamespace", out var targetNamespace)
            && !string.IsNullOrEmpty(targetNamespace)
            ? targetNamespace
            : compilation.AssemblyName;
    }
}
