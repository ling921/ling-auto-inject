using Ling.AutoInject.SourceGenerators.Resources;
using Microsoft.CodeAnalysis;

namespace Ling.AutoInject.SourceGenerators.Diagnostics;

/// <summary>
/// Provides diagnostic descriptors for the analyzers.
/// </summary>
internal static class DiagnosticDescriptors
{
    private static LocalizableResourceString L(string name) => new(name, SR.ResourceManager, typeof(SR));

    /// <summary>
    /// The diagnostic ID for rule <see cref="DuplicateAttributeRule"/>.
    /// </summary>
    public const string DuplicateAttributeId = "LAI001";

    /// <summary>
    /// The diagnostic ID for rule <see cref="ConflictingLifetimeRule"/>.
    /// </summary>
    public const string ConflictingLifetimeId = "LAI002";

    /// <summary>
    /// The diagnostic ID for rule <see cref="ServiceTypeMismatchRule"/>.
    /// </summary>
    public const string ServiceTypeMismatchId = "LAI003";

    /// <summary>
    /// The diagnostic ID for invalid AutoInjectConfig values.
    /// </summary>
    public const string InvalidConfigId = "LAI004";

    /// <summary>
    /// The diagnostic ID for rule <see cref="NotSupportedKeyedServiceRule"/>.
    /// </summary>
    public const string NotSupportedKeyedServiceId = "LAI101";

    /// <summary>
    /// The diagnostic ID for conflicting extension methods rule.
    /// </summary>
    public const string ConflictingExtensionId = "LAI102";

    /// <summary>
    /// The diagnostic rule for detecting duplicate attributes on a class.
    /// <para>
    /// Message format: <c>Duplicate service registration for same lifetime '{0}' found.</c>
    /// </para>
    /// </summary>
    public static readonly DiagnosticDescriptor DuplicateAttributeRule = new(
        id: DuplicateAttributeId,
        title: L(nameof(SR.DuplicateAttribute_Title)),
        messageFormat: L(nameof(SR.DuplicateAttribute_Message)),
        category: "Design",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// The diagnostic rule for detecting conflicting lifetimes for the same class.
    /// <para>
    /// Message format: <c>A service cannot be registered with both '{0}' and '{1}' lifetimes.</c>
    /// </para>
    /// </summary>
    public static readonly DiagnosticDescriptor ConflictingLifetimeRule = new(
        id: ConflictingLifetimeId,
        title: L(nameof(SR.ConflictingLifetime_Title)),
        messageFormat: L(nameof(SR.ConflictingLifetime_Message)),
        category: "Design",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// The diagnostic rule for detecting service type mismatches.
    /// <para>
    /// Message format: <c>The service type '{0}' is not an interface or is not implemented by '{1}'</c>
    /// </para>
    /// </summary>
    public static readonly DiagnosticDescriptor ServiceTypeMismatchRule = new(
        id: ServiceTypeMismatchId,
        title: L(nameof(SR.ServiceTypeMismatch_Title)),
        messageFormat: L(nameof(SR.ServiceTypeMismatch_Message)),
        category: "Design",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// The diagnostic rule for invalid AutoInject configuration values.
    /// <para>
    /// Message format: <c>AutoInjectConfig's {0} value '{1}' is not a valid {0}. It must be a valid C# identifier{2}.</c>
    /// </para>
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidConfigRule = new(
        id: InvalidConfigId,
        title: L(nameof(SR.InvalidConfig_Title)),
        messageFormat: L(nameof(SR.InvalidConfig_Message)),
        category: "Design",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// The diagnostic rule for not supported keyed service registrations.
    /// <para>
    /// Message format: <c>Keyed service registrations are supported only for Microsoft.Extensions.DependencyInjection.Abstractions version 8.0.0 or higher.</c>
    /// </para>
    /// </summary>
    public static readonly DiagnosticDescriptor NotSupportedKeyedServiceRule = new(
        id: NotSupportedKeyedServiceId,
        title: L(nameof(SR.NotSupportedKeyedService_Title)),
        messageFormat: L(nameof(SR.NotSupportedKeyedService_Message)),
        category: "Design",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// The diagnostic rule for detecting conflicting extension methods with the generated AutoInject extensions.
    /// <para>
    /// Message format: <c>An extension method named '{0}' with signature 'IServiceCollection Add(... this IServiceCollection services)' already exists and may conflict with the generated AutoInject extension.</c>
    /// </para>
    /// </summary>
    public static readonly DiagnosticDescriptor ConflictingExtensionRule = new(
        id: ConflictingExtensionId,
        title: L(nameof(SR.ConflictingExtension_Title)),
        messageFormat: L(nameof(SR.ConflictingExtension_Message)),
        category: "Design",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
