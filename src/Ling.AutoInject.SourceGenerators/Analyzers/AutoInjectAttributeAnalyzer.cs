using Ling.AutoInject.SourceGenerators.Diagnostics;
using Ling.AutoInject.SourceGenerators.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Ling.AutoInject.SourceGenerators.Analyzers;

/// <summary>
/// Analyzer that validates usages of AutoInject attributes applied to types.
/// <para>
/// Reports diagnostics for:
/// <list type="number">
/// <item>
/// Duplicate registrations for the same service type and lifetime,
/// </item>
/// <item>
/// Conflicting service lifetimes on the same implementation type,
/// </item>
/// <item>
/// Service type mismatches where the declared service type is not an interface or not implemented by the type.
/// </item>
/// </list>
/// Note: the analyzer may report multiple diagnostics for the same type when multiple issues are detected.
/// </para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class AutoInjectAttributeAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
        DiagnosticDescriptors.DuplicateAttributeRule,
        DiagnosticDescriptors.ConflictingLifetimeRule,
        DiagnosticDescriptors.ServiceTypeMismatchRule,
        DiagnosticDescriptors.UnsupportedRegistrationTargetRule,
        DiagnosticDescriptors.InvalidRegistrationOptionsRule,
    ];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol typeSymbol)
        {
            return;
        }

        var symbols = new AutoInjectSymbols(context.Compilation);
        var attrs = typeSymbol.GetAttributes()
            .Where(a => symbols.IsAutoInjectAttribute(a.AttributeClass))
            .ToList();
        if (attrs.Count == 0)
        {
            return;
        }

        var unsupportedKind = GetUnsupportedRegistrationKind(typeSymbol);
        if (unsupportedKind is not null)
        {
            foreach (var attr in attrs)
            {
                var location = attr.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation();
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.UnsupportedRegistrationTargetRule,
                    location,
                    unsupportedKind,
                    typeSymbol.Name));
            }

            return;
        }

        var cache = new Dictionary<string, List<AttributeInfo>>();
        foreach (var attr in attrs)
        {
            var location = attr.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation();

            // Conflicting lifetime check
            var lifetime = symbols.GetLifetime(attr.AttributeClass) ?? GetUnifiedLifetime(attr);
            if (lifetime is null)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.InvalidRegistrationOptionsRule,
                    location, "Lifetime must be Singleton, Scoped, or Transient"));
                continue;
            }
            var strategy = attr.GetNamedArgument("Strategy");
            if (strategy.Value is int strategyValue && (strategyValue < 0 || strategyValue > 3))
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.InvalidRegistrationOptionsRule,
                    location, "Strategy must be Add, TryAdd, Replace, or TryAddEnumerable"));
            }
            if (attr.GetNamedArgument("ServiceKey").Kind == TypedConstantKind.Array)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.InvalidRegistrationOptionsRule,
                    location, "ServiceKey cannot be an array because array keys use reference equality"));
            }
            var conflictingLifetime = cache.Keys.FirstOrDefault(lt => lt != lifetime);
            if (conflictingLifetime is not null && lifetime != conflictingLifetime)
            {
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.ConflictingLifetimeRule,
                    location,
                    conflictingLifetime,
                    lifetime);
                context.ReportDiagnostic(diagnostic);
            }

            var (serviceType, serviceKey) = GetAttributeArguments(attr, symbols);

            var interfaces = attr.GetNamedArgument("RegisterImplementedInterfaces").Value is true;
            var serviceTypes = new HashSet<INamedTypeSymbol?>(SymbolEqualityComparer.Default);
            if (interfaces) serviceTypes.UnionWith(typeSymbol.AllInterfaces);
            if (!interfaces || serviceType is not null) serviceTypes.Add(serviceType);
            if (serviceTypes.Count == 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.InvalidRegistrationOptionsRule,
                    location, "RegisterImplementedInterfaces requires at least one implemented interface"));
            }

            if (!cache.TryGetValue(lifetime, out var attrList)) cache[lifetime] = attrList = [];
            if (serviceTypes.Any(candidate => attrList.Any(a =>
                SymbolEqualityComparer.Default.Equals(a.ServiceType, candidate) && a.ServiceKey == serviceKey)))
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.DuplicateAttributeRule, location, lifetime));
            }
            attrList.AddRange(serviceTypes.Select(candidate => new AttributeInfo(candidate, serviceKey)));

            // Service type mismatch check
            if (serviceType is not null && !typeSymbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, serviceType)))
            {
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.ServiceTypeMismatchRule,
                    location,
                    serviceType.Name,
                    typeSymbol.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static (INamedTypeSymbol? ServiceType, string? ServiceKey) GetAttributeArguments(AttributeData attributeData, AutoInjectSymbols symbols)
    {
        INamedTypeSymbol? serviceType = null;
        string? serviceKey = null;
        foreach (var item in attributeData.NamedArguments)
        {
            if (item.Key == "ServiceType")
            {
                if (!item.Value.IsNull)
                {
                    serviceType = item.Value.Value as INamedTypeSymbol;
                }
            }
            else if (item.Key == "ServiceKey")
            {
                if (!item.Value.IsNull)
                {
                    serviceKey = item.Value.ToCSharpString();
                }
            }
        }

        var serviceTypeArgumentIndex = SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, symbols.AutoInjectAttributeSymbol)
            ? 1
            : 0;
        if (attributeData.ConstructorArguments.Length > serviceTypeArgumentIndex)
        {
            var serviceTypeArgument = attributeData.ConstructorArguments[serviceTypeArgumentIndex];
            if (!serviceTypeArgument.IsNull && !attributeData.NamedArguments.Any(a => a.Key == "ServiceType"))
            {
                serviceType = serviceTypeArgument.Value as INamedTypeSymbol;
            }
        }

        return (serviceType, serviceKey);
    }

    private static string? GetUnifiedLifetime(AttributeData attributeData)
    {
        if (attributeData.ConstructorArguments.Length == 0)
        {
            return null;
        }

        return attributeData.ConstructorArguments[0].Value switch
        {
            0 => "Singleton",
            1 => "Scoped",
            2 => "Transient",
            _ => null,
        };
    }

    private static string? GetUnsupportedRegistrationKind(INamedTypeSymbol typeSymbol)
    {
        if (typeSymbol.IsStatic)
        {
            return "static";
        }

        if (typeSymbol.IsAbstract)
        {
            return "abstract";
        }

        if (HasTypeParameters(typeSymbol))
        {
            return "generic";
        }

        return typeSymbol.TypeKind != TypeKind.Class ? "non-class" : null;
    }

    private static bool HasTypeParameters(INamedTypeSymbol typeSymbol)
    {
        for (var current = typeSymbol; current is not null; current = current.ContainingType)
        {
            if (current.TypeParameters.Length > 0)
            {
                return true;
            }
        }

        return false;
    }

    private sealed record AttributeInfo(INamedTypeSymbol? ServiceType, string? ServiceKey);
}
