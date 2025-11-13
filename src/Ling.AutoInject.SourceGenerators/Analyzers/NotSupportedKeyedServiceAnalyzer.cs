using Ling.AutoInject.SourceGenerators.Diagnostics;
using Ling.AutoInject.SourceGenerators.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Ling.AutoInject.SourceGenerators.Analyzers;

/// <summary>
/// Analyzer to report diagnostic when keyed services are used in unsupported versions.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class NotSupportedKeyedServiceAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
        DiagnosticDescriptors.NotSupportedKeyedServiceRule,
    ];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.AttributeArgument);
    }

    private void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not AttributeArgumentSyntax attributeArgument)
        {
            return;
        }

        var version = context.SemanticModel.Compilation.FindReferenceAssemblyVersionByTypeMetadataName(Constants.ServiceCollectionServiceExtensionsFullName);
        if (version >= Constants.SupportKeyedServiceVersion)
        {
            return;
        }

        // 1. Attribute argument must have a name equal to "ServiceKey"
        // 2. Attribute type must be one of AutoInject attributes

        if (attributeArgument.NameEquals?.Name.Identifier.Text != "ServiceKey")
        {
            return;
        }

        var attributeSyntax = attributeArgument.FirstAncestorOrSelf<AttributeSyntax>();
        if (attributeSyntax is null)
        {
            return;
        }

        var attributeSymbol = context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol;
        var attributeTypeSymbol = attributeSymbol?.ContainingType;
        var symbols = new AutoInjectSymbols(context.SemanticModel.Compilation);
        if (!symbols.IsAutoInjectAttribute(attributeTypeSymbol))
        {
            return;
        }

        var diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.NotSupportedKeyedServiceRule,
            attributeArgument.GetLocation());
        context.ReportDiagnostic(diagnostic);
    }
}
