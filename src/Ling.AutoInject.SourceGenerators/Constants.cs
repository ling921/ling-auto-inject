namespace Ling.AutoInject.SourceGenerators;

internal static class Constants
{
    public static string Version = typeof(Constants).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";

    public static Version SupportKeyedServiceVersion = new(8, 0, 0);

    public const string ServiceCollectionServiceExtensionsFullName = "Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions";

    public const string AutoInjectConfigAttributeFullName = "Ling.AutoInject.AutoInjectConfigAttribute";

    public const string TransientServiceAttributeFullName = "Ling.AutoInject.TransientServiceAttribute";
    public const string ScopedServiceAttributeFullName = "Ling.AutoInject.ScopedServiceAttribute";
    public const string SingletonServiceAttributeFullName = "Ling.AutoInject.SingletonServiceAttribute";
}
