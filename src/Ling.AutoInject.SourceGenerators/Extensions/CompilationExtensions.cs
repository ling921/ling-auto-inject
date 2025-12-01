using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Ling.AutoInject.SourceGenerators.Extensions;

internal static class CompilationExtensions
{
    extension(Compilation compilation)
    {
        public Version? FindReferenceAssemblyVersionByTypeMetadataName(string typeMetadataName)
        {
            var typeSymbol = compilation.GetTypeByMetadataName(typeMetadataName);
            return typeSymbol?.ContainingAssembly.Identity.Version;
        }

        public string? GetNamespace(AnalyzerConfigOptionsProvider optionsProvider)
        {
            return optionsProvider.GlobalOptions.TryGetValue("build_property.TargetNamespace", out var targetNamespace)
                && !string.IsNullOrEmpty(targetNamespace)
                ? targetNamespace
                : compilation.AssemblyName;
        }

        public bool HasClassWithAttribute(INamedTypeSymbol attributeSymbol)
        {
            foreach (var tree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(tree);
                var root = tree.GetRoot();

                foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    var typeSymbol = semanticModel.GetDeclaredSymbol(classDecl);
                    if (typeSymbol is INamedTypeSymbol classSymbol
                        && classSymbol.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attributeSymbol)))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
