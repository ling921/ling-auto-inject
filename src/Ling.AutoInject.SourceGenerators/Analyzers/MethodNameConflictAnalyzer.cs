using Ling.AutoInject.SourceGenerators.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Ling.AutoInject.SourceGenerators.Analyzers;

/// <summary>
/// Analyzer that verifies the user-configured AutoInject extension method name does not collide with an existing extension method.
/// <para>
/// Reports diagnostics when a static extension method with the same name and signature already exists.
/// </para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class MethodNameConflictAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [DiagnosticDescriptors.ConflictingExtensionRule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compStart =>
        {
            var compilation = compStart.Compilation;
            var symbols = new AutoInjectSymbols(compilation);
            var attrData = FindExtensionsAttribute(compilation.Assembly.GlobalNamespace, symbols.AutoInjectExtensionsAttributeSymbol)
                ?? compilation.Assembly.GetAttributes()
                    .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, symbols.AutoInjectConfigAttributeSymbol));

            var methodName = GetConfiguredMethodName(attrData);
            if (string.IsNullOrEmpty(methodName) || attrData is null)
                return;

            // Resolve IServiceCollection type symbol to compare parameter types
            var svcCollectionType = compilation.GetTypeByMetadataName("Microsoft.Extensions.DependencyInjection.IServiceCollection");
            if (svcCollectionType is null)
                return;

            // Register symbol action to inspect method symbols
            compStart.RegisterSymbolAction(symCtx =>
            {
                var method = (IMethodSymbol)symCtx.Symbol;

                if (method.Name != methodName)
                    return;

                // Only consider user-declared methods (ignore compiler-generated etc.)
                if (method.Locations.Length == 0)
                    return;

                // Must be static and an extension method (uses 'this' on first parameter)
                if (!method.IsStatic || !method.IsExtensionMethod)
                    return;

                if (method.Parameters.Length == 0)
                    return;

                var firstParamType = method.Parameters[0].Type;
                if (!SymbolEqualityComparer.Default.Equals(firstParamType, svcCollectionType))
                    return;

                // Determine diagnostic location: prefer the MethodName named-argument expression location
                Location? diagLocation = null;

                if (attrData.ApplicationSyntaxReference != null)
                {
                    var attrSyntax = (AttributeSyntax)attrData.ApplicationSyntaxReference.GetSyntax(symCtx.CancellationToken);
                    if (attrSyntax.ArgumentList is not null)
                    {
                        foreach (var arg in attrSyntax.ArgumentList.Arguments)
                        {
                            if (arg.NameEquals?.Name.Identifier.ValueText == "MethodName")
                            {
                                diagLocation = arg.GetLocation();
                                break;
                            }
                        }
                    }
                }

                // Fallback to attribute location (whole attribute) if specific arg location not found
                if (diagLocation is null && attrData.ApplicationSyntaxReference is not null)
                {
                    diagLocation = attrData.ApplicationSyntaxReference.GetSyntax(symCtx.CancellationToken).GetLocation();
                }

                // If we still don't have a location, use the method location
                diagLocation ??= method.Locations[0];

                var diagnostic = Diagnostic.Create(DiagnosticDescriptors.ConflictingExtensionRule, diagLocation, methodName);
                symCtx.ReportDiagnostic(diagnostic);

            }, SymbolKind.Method);
        });
    }

    private static AttributeData? FindExtensionsAttribute(INamespaceSymbol namespaceSymbol, INamedTypeSymbol attributeSymbol)
    {
        foreach (var type in namespaceSymbol.GetTypeMembers())
        {
            var attribute = type.GetAttributes()
                .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attributeSymbol));
            if (attribute is not null)
            {
                return attribute;
            }
        }

        foreach (var childNamespace in namespaceSymbol.GetNamespaceMembers())
        {
            var attribute = FindExtensionsAttribute(childNamespace, attributeSymbol);
            if (attribute is not null)
            {
                return attribute;
            }
        }

        return null;
    }

    private static string? GetConfiguredMethodName(AttributeData? attributeData)
    {
        if (attributeData is null)
        {
            return null;
        }

        foreach (var named in attributeData.NamedArguments)
        {
            if (named.Key == "MethodName" && !named.Value.IsNull)
            {
                return named.Value.ToCSharpString().Trim('"');
            }
        }

        return null;
    }
}
