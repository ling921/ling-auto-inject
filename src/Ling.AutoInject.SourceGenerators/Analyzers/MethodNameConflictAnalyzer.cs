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
            var attrData = compilation.Assembly.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == Constants.AutoInjectConfigAttributeFullName);

            if (attrData is null)
                return;

            string? methodName = null;

            // Prefer named argument MethodName
            foreach (var named in attrData.NamedArguments)
            {
                if (named.Key == "MethodName" && !named.Value.IsNull)
                {
                    methodName = named.Value.ToCSharpString().Trim('"');
                    break;
                }
            }

            if (string.IsNullOrEmpty(methodName))
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
}
