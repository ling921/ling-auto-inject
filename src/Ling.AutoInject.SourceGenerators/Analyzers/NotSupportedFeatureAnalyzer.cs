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
internal sealed class NotSupportedFeatureAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
        DiagnosticDescriptors.NotSupportedKeyedServiceRule,
        DiagnosticDescriptors.NotSupportedReplaceServiceRule,
        DiagnosticDescriptors.RequiredServiceTypeForReplaceRule,
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
            if (attributeArgument.NameEquals?.Name.Identifier.Text is not "Replace")
            {
                return;
            }

            var expression = attributeArgument.Expression;
            if (expression is not LiteralExpressionSyntax literalExpression ||
                literalExpression.IsKind(SyntaxKind.FalseLiteralExpression))
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

            if (!HasServiceType(attributeSyntax))
            {
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.RequiredServiceTypeForReplaceRule,
                    attributeArgument.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
        else
        {
            if (attributeArgument.NameEquals?.Name.Identifier.Text is not "ServiceKey" and not "Replace")
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

            var diagnostic = attributeArgument.NameEquals?.Name.Identifier.Text is "ServiceKey"
                ? Diagnostic.Create(
                    DiagnosticDescriptors.NotSupportedKeyedServiceRule,
                    attributeArgument.GetLocation())
                : Diagnostic.Create(
                    DiagnosticDescriptors.NotSupportedReplaceServiceRule,
                    attributeArgument.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool HasServiceType(AttributeSyntax attributeSyntax)
    {
        var hasCtorArgs = false;
        var hasServiceTypeNamedArg = false;

        var args = attributeSyntax.ArgumentList?.Arguments;
        if (args == null)
            return false;

        foreach (var arg in args)
        {
            if (arg.NameEquals == null)
            {
                hasCtorArgs = true;
            }
            else if (arg.NameEquals.Name.Identifier.Text == "ServiceType")
            {
                hasServiceTypeNamedArg = true;
            }
        }

        return hasCtorArgs || hasServiceTypeNamedArg;
    }
}
