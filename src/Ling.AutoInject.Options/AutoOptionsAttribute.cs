namespace Ling.AutoInject.Options;

/// <summary>Registers an options type and binds it from a configuration section.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class AutoOptionsAttribute(string sectionPath) : Attribute
{
    /// <summary>Gets the configuration section path.</summary>
    public string SectionPath { get; } = sectionPath;

    /// <summary>Gets or sets the named-options name; null or empty selects the default name.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets whether DataAnnotations validation is registered.</summary>
    public bool ValidateDataAnnotations { get; set; }

    /// <summary>Gets or sets whether validation runs on application start.</summary>
    public bool ValidateOnStart { get; set; }

    /// <summary>Gets or sets the module type that owns this registration.</summary>
    public Type? Module { get; set; }
}
