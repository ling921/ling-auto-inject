using Ling.AutoInject.SourceGenerators.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Ling.AutoInject.SourceGenerators.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class AutoOptionsAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        DiagnosticDescriptors.InvalidOptionsPathRule, DiagnosticDescriptors.DuplicateOptionsRule,
        DiagnosticDescriptors.UnsupportedOptionsFeatureRule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(start =>
        {
            var attribute = start.Compilation.GetTypeByMetadataName(Constants.AutoOptionsAttributeFullName);
            if (attribute is null) return;
            var registrations = new ConcurrentDictionary<(INamedTypeSymbol, string), Location>(new OptionsKeyComparer());
            var optionsVersion = start.Compilation.GetTypeByMetadataName("Microsoft.Extensions.Options.OptionsBuilder`1")?.ContainingAssembly.Identity.Version;
            start.RegisterSymbolAction(symbolContext =>
            {
                var type = (INamedTypeSymbol)symbolContext.Symbol;
                foreach (var data in type.GetAttributes().Where(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attribute)))
                {
                    var location = data.ApplicationSyntaxReference?.GetSyntax(symbolContext.CancellationToken).GetLocation() ?? type.Locations[0];
                    var path = data.ConstructorArguments.FirstOrDefault().Value as string;
                    if (!IsValidPath(path)) symbolContext.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.InvalidOptionsPathRule, location, path ?? ""));
                    var name = data.NamedArguments.FirstOrDefault(a => a.Key == "Name").Value.Value as string ?? string.Empty;
                    if (!registrations.TryAdd((type, name), location)) symbolContext.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.DuplicateOptionsRule, location, type.Name, string.IsNullOrEmpty(name) ? "default" : name));
                    if (data.NamedArguments.FirstOrDefault(a => a.Key == "ValidateOnStart").Value.Value is true && optionsVersion is { Major: < 6 })
                        symbolContext.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.UnsupportedOptionsFeatureRule, location, "ValidateOnStart requires Microsoft.Extensions.Options 6.0 or later."));
                    if (data.NamedArguments.FirstOrDefault(a => a.Key == "ValidateDataAnnotations").Value.Value is true && start.Compilation.GetTypeByMetadataName("Microsoft.Extensions.DependencyInjection.OptionsBuilderDataAnnotationsExtensions") is null)
                        symbolContext.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.UnsupportedOptionsFeatureRule, location, "ValidateDataAnnotations requires Microsoft.Extensions.Options.DataAnnotations."));
                }
            }, SymbolKind.NamedType);
        });
    }

    private static bool IsValidPath(string? path) => !string.IsNullOrWhiteSpace(path)
        && path![0] != ':' && path[^1] != ':'
        && path.Split(':').All(segment => !string.IsNullOrWhiteSpace(segment) && segment == segment.Trim());

    private sealed class OptionsKeyComparer : IEqualityComparer<(INamedTypeSymbol, string)>
    {
        public bool Equals((INamedTypeSymbol, string) x, (INamedTypeSymbol, string) y) => SymbolEqualityComparer.Default.Equals(x.Item1, y.Item1) && x.Item2 == y.Item2;
        public int GetHashCode((INamedTypeSymbol, string) obj) => (SymbolEqualityComparer.Default.GetHashCode(obj.Item1) * 397) ^ obj.Item2.GetHashCode();
    }
}
