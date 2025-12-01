using Ling.AutoInject.SourceGenerators.Diagnostics;
using Ling.AutoInject.SourceGenerators.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Ling.AutoInject.SourceGenerators.Analyzers;

/// <summary>
/// Analyzer for 'AutoInjectExtensionsAttribute'.
/// <para>
/// Reports diagnostics for:
/// <list type="number">
/// <item>
/// Method name is not a valid C# identifier.
/// </item>
/// <item>
/// Multiple 'AutoInjectExtensionsAttribute' found in the same assembly.
/// </item>
/// <item>
/// Target class not declared as static partial.
/// </item>
/// </list>
/// </para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class AutoInjectExtensionsAttributeAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
        DiagnosticDescriptors.InvalidNamingRule,
        DiagnosticDescriptors.MultipleExtensionsUsedRule,
        DiagnosticDescriptors.RequiredStaticPartialClassRule,
    ];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compStart =>
        {
            var compilation = compStart.Compilation;
            var symbols = new AutoInjectSymbols(compilation);

            var foundClasses = new ConcurrentBag<(INamedTypeSymbol, Location)>();

            compStart.RegisterSyntaxNodeAction(ctx => AnalyzeAttribute(ctx, symbols, foundClasses), SyntaxKind.Attribute);

            compStart.RegisterCompilationEndAction(cac => CompileEndAction(cac, foundClasses));
        });
    }

    private void AnalyzeAttribute(SyntaxNodeAnalysisContext context, AutoInjectSymbols autoInjectSymbols, ConcurrentBag<(INamedTypeSymbol, Location)> foundClasses)
    {
        var attributeSyntax = (AttributeSyntax)context.Node;

        var attributeType = context.SemanticModel.GetTypeInfo(attributeSyntax, context.CancellationToken).Type;
        if (!SymbolEqualityComparer.Default.Equals(attributeType, autoInjectSymbols.AutoInjectExtensionsAttributeSymbol))
        {
            return;
        }

        if (attributeSyntax.ArgumentList is not null)
        {
            foreach (var arg in attributeSyntax.ArgumentList.Arguments.Where(a => a.NameEquals?.Name.Identifier.Text == "MethodName"))
            {
                var constantValue = context.SemanticModel.GetConstantValue(arg.Expression, context.CancellationToken);
                if (constantValue.HasValue
                    && constantValue.Value is string methodName
                    && !CSharpIdentifierHelper.IsValidIdentifierAllowAt(methodName))
                {
                    var diagnostic = Diagnostic.Create(DiagnosticDescriptors.InvalidNamingRule, arg.GetLocation(), methodName, "method name");
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        var classDeclaration = attributeSyntax.FirstAncestorOrSelf<ClassDeclarationSyntax>();
        if (classDeclaration is null)
            return;

        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);
        if (classSymbol is null)
            return;

        foundClasses.Add((classSymbol, classDeclaration.Identifier.GetLocation()));

        // Check for static partial class
        if (!classDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword) || !classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.RequiredStaticPartialClassRule,
                classDeclaration.Identifier.GetLocation(),
                classSymbol.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private void CompileEndAction(
        CompilationAnalysisContext context,
        ConcurrentBag<(INamedTypeSymbol TypeSymbol, Location Location)> foundClasses)
    {
        // Check for multiple usages
        if (foundClasses.Count > 1)
        {
            // Order by file path and position to have deterministic diagnostics
            var list = foundClasses
                .OrderBy(t => t.Location.SourceTree?.FilePath ?? string.Empty, StringComparer.Ordinal)
                .ThenBy(t => t.Location.SourceSpan.Start)
                .ToList();
            foreach (var (_, location) in list.Skip(1))
            {
                var diagnostic = Diagnostic.Create(DiagnosticDescriptors.MultipleExtensionsUsedRule, location);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
