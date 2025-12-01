using Ling.AutoInject.SourceGenerators.Diagnostics;
using Ling.AutoInject.SourceGenerators.Extensions;
using Ling.AutoInject.SourceGenerators.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Ling.AutoInject.SourceGenerators.Analyzers;

/// <summary>
/// Analyzer that validates assembly-level AutoInjectConfig attribute values (MethodName, ClassName, Namespace).
/// <para>
/// Reports diagnostics for:
/// <list type="number">
/// <item>
/// Arguments are not valid C# identifiers or contain invalid characters.
/// </item>
/// <item>
/// Unnecessary usage of 'AutoInjectConfigAttribute' when 'AutoInjectExtensionsAttribute' is present.
/// </item>
/// </list>
/// </para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class InvalidAutoInjectConfigAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
        DiagnosticDescriptors.InvalidNamingRule,
        DiagnosticDescriptors.UnnecessaryConfigUsageRule,
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeAttribute, SyntaxKind.Attribute);
    }

    private void AnalyzeAttribute(SyntaxNodeAnalysisContext ctx)
    {
        var attrSyntax = (AttributeSyntax)ctx.Node;

        // Only assembly-level attributes
        if (attrSyntax.Parent is not AttributeListSyntax attrList || attrList.Target?.Identifier.ValueText != "assembly")
            return;

        var semanticModel = ctx.SemanticModel;
        var symbols = new AutoInjectSymbols(semanticModel.Compilation);
        var attrType = semanticModel.GetTypeInfo(attrSyntax, ctx.CancellationToken).Type;
        if (!SymbolEqualityComparer.Default.Equals(attrType, symbols.AutoInjectConfigAttributeSymbol))
            return;

        if (attrSyntax.ArgumentList is not null)
        {
            foreach (var arg in attrSyntax.ArgumentList.Arguments)
            {
                if (arg.NameEquals is not null)
                {
                    var name = arg.NameEquals.Name.Identifier.ValueText;
                    var constVal = semanticModel.GetConstantValue(arg.Expression, ctx.CancellationToken);
                    if (!constVal.HasValue
                        || constVal.Value is not string value
                        || string.IsNullOrEmpty(value))
                    {
                        continue;
                    }

                    switch (name)
                    {
                        case "MethodName":
                            if (!CSharpIdentifierHelper.IsValidIdentifierAllowAt(value))
                            {
                                var diagnostic = Diagnostic.Create(
                                    DiagnosticDescriptors.InvalidNamingRule,
                                    arg.Expression.GetLocation(),
                                    value,
                                    "method name");
                                ctx.ReportDiagnostic(diagnostic);
                            }
                            break;

                        case "ClassName":
                            if (!CSharpIdentifierHelper.IsValidIdentifierAllowAt(value))
                            {
                                var diagnostic = Diagnostic.Create(
                                    DiagnosticDescriptors.InvalidNamingRule,
                                    arg.Expression.GetLocation(),
                                    value,
                                    "class name");
                                ctx.ReportDiagnostic(diagnostic);
                            }
                            break;

                        case "Namespace":
                            if (!CSharpIdentifierHelper.IsValidNamespace(value))
                            {
                                var diagnostic = Diagnostic.Create(
                                    DiagnosticDescriptors.InvalidNamingRule,
                                    arg.Expression.GetLocation(),
                                    value,
                                    "namespace");
                                ctx.ReportDiagnostic(diagnostic);
                            }
                            break;
                    }
                }
            }
        }

        if (semanticModel.Compilation.HasClassWithAttribute(symbols.AutoInjectExtensionsAttributeSymbol))
        {
            var diagnostic = Diagnostic.Create(DiagnosticDescriptors.UnnecessaryConfigUsageRule, attrSyntax.GetLocation());
            ctx.ReportDiagnostic(diagnostic);
        }
    }
}
