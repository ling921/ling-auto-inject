using Ling.AutoInject.SourceGenerators.Diagnostics;
using Ling.AutoInject.SourceGenerators.Extensions;
using Ling.AutoInject.SourceGenerators.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Ling.AutoInject.SourceGenerators.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class AutoInjectModuleAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        DiagnosticDescriptors.InvalidNamingRule,
        DiagnosticDescriptors.RequiredStaticPartialClassRule,
        DiagnosticDescriptors.InvalidModuleReferenceRule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
    }

    private static void AnalyzeType(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        var symbols = new AutoInjectSymbols(context.Compilation);
        var moduleAttribute = type.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, symbols.AutoInjectModuleAttributeSymbol));
        if (moduleAttribute is not null)
        {
            var location = moduleAttribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation() ?? type.Locations[0];
            if (!type.IsStatic || !type.DeclaringSyntaxReferences.Any(r => r.GetSyntax(context.CancellationToken) is ClassDeclarationSyntax c && c.Modifiers.Any(SyntaxKind.PartialKeyword)))
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.RequiredStaticPartialClassRule, location, type.Name));
            }
            if (type.ContainingType is not null || type.TypeParameters.Length != 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.InvalidModuleReferenceRule, location, "An AutoInject module must be a non-generic top-level class."));
            }
            var methodName = moduleAttribute.GetNamedArgument("MethodName").Value as string;
            if (!string.IsNullOrWhiteSpace(methodName) && !CSharpIdentifierHelper.IsValidIdentifierAllowAt(methodName))
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.InvalidNamingRule, location, methodName, "method name"));
            }
        }

        foreach (var attribute in type.GetAttributes().Where(a => symbols.IsAutoInjectAttribute(a.AttributeClass)).Concat(GetOptionsAttributes(type, symbols)))
        {
            var target = attribute.GetNamedArgument("Module").Value as INamedTypeSymbol;
            if (target is null) continue;
            var targetAttribute = target.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, symbols.AutoInjectModuleAttributeSymbol));
            if (targetAttribute is null || !target.IsStatic || target.ContainingType is not null || target.TypeParameters.Length != 0
                || !target.DeclaringSyntaxReferences.Any(r => r.GetSyntax(context.CancellationToken) is ClassDeclarationSyntax c && c.Modifiers.Any(SyntaxKind.PartialKeyword)))
            {
                var location = attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation() ?? type.Locations[0];
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.InvalidModuleReferenceRule, location, $"Module '{target.Name}' must be a non-generic top-level static partial class marked with [AutoInjectModule]."));
            }
        }
    }

    private static IEnumerable<AttributeData> GetOptionsAttributes(INamedTypeSymbol type, AutoInjectSymbols symbols) =>
        symbols.AutoOptionsAttributeSymbol is null
            ? Enumerable.Empty<AttributeData>()
            : type.GetAttributes().Where(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, symbols.AutoOptionsAttributeSymbol));
}
