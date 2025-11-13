using Ling.AutoInject.SourceGenerators.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace Ling.AutoInject.SourceGenerators.Analyzers;

/// <summary>
/// Analyzer that validates assembly-level AutoInjectConfig attribute values (MethodName, ClassName, Namespace).
/// <para>
/// Reports diagnostics when values are not valid C# identifiers or contain invalid characters.
/// </para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class InvalidAutoInjectConfigAnalyzer : DiagnosticAnalyzer
{
    // Allow @-prefixed identifiers for MethodName/ClassName (C# allows @identifier),
    // but namespace segments must NOT use @, so use a separate regex for namespace segments.
    private static readonly Regex _identifierRegex = new("^@?[a-zA-Z_][a-zA-Z0-9_]*$");
    private static readonly Regex _namespaceSegmentRegex = new("^[a-zA-Z_][a-zA-Z0-9_]*$");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [DiagnosticDescriptors.InvalidConfigRule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeAttribute, Microsoft.CodeAnalysis.CSharp.SyntaxKind.Attribute);
    }

    private void AnalyzeAttribute(SyntaxNodeAnalysisContext ctx)
    {
        var attrSyntax = (AttributeSyntax)ctx.Node;

        // Only assembly-level attributes
        if (attrSyntax.Parent is not AttributeListSyntax attrList || attrList.Target?.Identifier.ValueText != "assembly")
            return;

        var semanticModel = ctx.SemanticModel;
        var attrType = semanticModel.GetTypeInfo(attrSyntax, ctx.CancellationToken).Type;
        if (attrType is null)
            return;

        if (attrType.ToDisplayString() != Constants.AutoInjectConfigAttributeFullName)
            return;

        // Helper to validate a value and report on the argument expression location
        void ValidateAndReport(string value, string kind, ExpressionSyntax expr)
        {
            if (string.IsNullOrEmpty(value))
                return;

            if (kind == "Namespace")
            {
                var segments = value.Split('.');
                for (int idx = 0; idx < segments.Length; idx++)
                {
                    if (!_namespaceSegmentRegex.IsMatch(segments[idx]))
                    {
                        var diagnostic = Diagnostic.Create(DiagnosticDescriptors.InvalidConfigRule, expr.GetLocation(), kind, value);
                        ctx.ReportDiagnostic(diagnostic);
                        return;
                    }
                }
                return;
            }

            if (!_identifierRegex.IsMatch(value))
            {
                var diagnostic = Diagnostic.Create(DiagnosticDescriptors.InvalidConfigRule, expr.GetLocation(), kind, value);
                ctx.ReportDiagnostic(diagnostic);
            }
        }

        // Read named arguments first
        string? methodName = null;
        string? className = null;
        string? @namespace = null;

        if (attrSyntax.ArgumentList is not null)
        {
            int positionalIndex = 0;
            foreach (var arg in attrSyntax.ArgumentList.Arguments)
            {
                // Named argument
                if (arg.NameEquals is not null)
                {
                    var name = arg.NameEquals.Name.Identifier.ValueText;
                    var constVal = semanticModel.GetConstantValue(arg.Expression, ctx.CancellationToken);
                    var str = constVal.HasValue && constVal.Value is string s ? s : null;
                    switch (name)
                    {
                        case "MethodName":
                            methodName = str;
                            if (str is not null) ValidateAndReport(str, "MethodName", arg.Expression);
                            break;
                        case "ClassName":
                            className = str;
                            if (str is not null) ValidateAndReport(str, "ClassName", arg.Expression);
                            break;
                        case "Namespace":
                            @namespace = str;
                            if (str is not null) ValidateAndReport(str, "Namespace", arg.Expression);
                            break;
                    }
                }
                else
                {
                    // Positional constructor argument: order (MethodName, ClassName, Namespace)
                    var constVal = semanticModel.GetConstantValue(arg.Expression, ctx.CancellationToken);
                    var str = constVal.HasValue && constVal.Value is string s ? s : null;
                    switch (positionalIndex)
                    {
                        case 0:
                            if (methodName is null && str is not null)
                            {
                                methodName = str;
                                ValidateAndReport(str, "MethodName", arg.Expression);
                            }
                            break;
                        case 1:
                            if (className is null && str is not null)
                            {
                                className = str;
                                ValidateAndReport(str, "ClassName", arg.Expression);
                            }
                            break;
                        case 2:
                            if (@namespace is null && str is not null)
                            {
                                @namespace = str;
                                ValidateAndReport(str, "Namespace", arg.Expression);
                            }
                            break;
                    }

                    positionalIndex++;
                }
            }
        }
    }
}
