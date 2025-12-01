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
    /// Diagnostic ID for rule <see cref="DuplicateAttributeRule"/>.
    /// </summary>
    public const string DuplicateAttributeId = "LAI001";

    /// <summary>
    /// Diagnostic ID for rule <see cref="ConflictingLifetimeRule"/>.
    /// </summary>
    public const string ConflictingLifetimeId = "LAI002";

    /// <summary>
    /// Diagnostic ID for rule <see cref="ServiceTypeMismatchRule"/>.
    /// </summary>
    public const string ServiceTypeMismatchId = "LAI003";

    /// <summary>
    /// Diagnostic ID for invalid naming of methods, classes, or namespaces.
    /// </summary>
    public const string InvalidNamingId = "LAI004";

    /// <summary>
    /// Diagnostic ID for rule <see cref="RequiredServiceTypeForReplaceRule"/>.
    /// </summary>
    public const string RequiredServiceTypeForReplaceId = "LAI005";

    /// <summary>
    /// Diagnostic ID for rule <see cref="MultipleExtensionsUsedRule"/>.
    /// </summary>
    public const string MultipleExtensionsUsedId = "LAI006";

    /// <summary>
    /// Diagnostic ID for rule <see cref="RequiredStaticPartialClassRule"/>.
    /// </summary>
    public const string RequiredStaticPartialClassId = "LAI007";

    /// <summary>
    /// Diagnostic ID for rule <see cref="NotSupportedKeyedServiceRule"/>.
    /// </summary>
    public const string NotSupportedKeyedServiceId = "LAI101";

    /// <summary>
    /// Diagnostic ID for rule <see cref="ConflictingExtensionRule"/>.
    /// </summary>
    public const string ConflictingExtensionId = "LAI102";

    /// <summary>
    /// Diagnostic ID for rule <see cref="NotSupportedReplaceServiceRule"/>.
    /// </summary>
    public const string NotSupportedReplaceServiceId = "LAI103";

    /// <summary>
    /// Diagnostic ID for rule <see cref="UnnecessaryConfigUsageRule"/>.
    /// </summary>
    public const string UnnecessaryConfigUsageId = "LAI104";

    /// <summary>
    /// Diagnostic rule for detecting duplicate attributes on a class.
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
    /// Diagnostic rule for detecting conflicting lifetimes for the same class.
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
    /// Diagnostic rule for detecting service type mismatches.
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
    /// Diagnostic rule for invalid naming for method, class and namespace.
    /// <para>
    /// Message format: <c>'{0}' is not a valid {1}. A valid C# identifier is required.</c>
    /// </para>
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidNamingRule = new(
        id: InvalidNamingId,
        title: L(nameof(SR.InvalidNaming_Title)),
        messageFormat: L(nameof(SR.InvalidNaming_Message)),
        category: "Design",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic rule for required service type for replace registrations.
    /// <para>
    /// Message format: <c>A service type must be specified when using Replace service registration.</c>
    /// </para>
    /// </summary>
    public static readonly DiagnosticDescriptor RequiredServiceTypeForReplaceRule = new(
        id: RequiredServiceTypeForReplaceId,
        title: L(nameof(SR.RequiredServiceTypeForReplace_Title)),
        messageFormat: L(nameof(SR.RequiredServiceTypeForReplace_Message)),
        category: "Design",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic rule for detecting multiple 'AutoInjectExtensionsAttribute' used in the same assembly.
    /// <para>
    /// Message format: <c>Multiple AutoInjectExtensionsAttribute found in the assembly. Only one is allowed.</c>
    /// </para>
    /// </summary>
    public static readonly DiagnosticDescriptor MultipleExtensionsUsedRule = new(
        id: MultipleExtensionsUsedId,
        title: L(nameof(SR.MultipleExtensionsUsed_Title)),
        messageFormat: L(nameof(SR.MultipleExtensionsUsed_Message)),
        category: "Design",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic rule for required static partial class for generation.
    /// <para>
    /// Message format: <c>The class '{0}' must be declared as static partial to generate code.</c>
    /// </para>
    /// </summary>
    public static readonly DiagnosticDescriptor RequiredStaticPartialClassRule = new(
        id: RequiredStaticPartialClassId,
        title: L(nameof(SR.RequiredStaticPartialClass_Title)),
        messageFormat: L(nameof(SR.RequiredStaticPartialClass_Message)),
        category: "Design",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic rule for not supported keyed service registrations.
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
    /// Diagnostic rule for detecting conflicting extension methods with the generated AutoInject extensions.
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

    /// <summary>
    /// Diagnostic rule for not supported replace service registrations.
    /// <para>
    /// Message format: <c>Replace service registrations are supported only for Microsoft.Extensions.DependencyInjection.Abstractions version 8.0.0 or higher.</c>
    /// </para>
    /// </summary>
    public static readonly DiagnosticDescriptor NotSupportedReplaceServiceRule = new(
        id: NotSupportedReplaceServiceId,
        title: L(nameof(SR.NotSupportedReplaceService_Title)),
        messageFormat: L(nameof(SR.NotSupportedReplaceService_Message)),
        category: "Design",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// Diagnostic rule for unnecessary usage of 'AutoInjectConfigAttribute' when 'AutoInjectExtensionsAttribute' is present.
    /// <para>
    /// Message format: <c>The 'AutoInjectConfigAttribute' is unnecessary when 'AutoInjectExtensionsAttribute' is present in the assembly.</c>
    /// </para>
    /// </summary>
    public static readonly DiagnosticDescriptor UnnecessaryConfigUsageRule = new(
        id: UnnecessaryConfigUsageId,
        title: L(nameof(SR.UnnecessaryConfigUsage_Title)),
        messageFormat: L(nameof(SR.UnnecessaryConfigUsage_Message)),
        category: "Design",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
