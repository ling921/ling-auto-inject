using Microsoft.CodeAnalysis;

namespace Ling.AutoInject.SourceGenerators;

internal sealed class AutoInjectSymbols(Compilation compilation)
{
    public INamedTypeSymbol AutoInjectConfigAttributeSymbol { get; } = compilation.GetTypeByMetadataName(Constants.AutoInjectConfigAttributeFullName)!;
    public INamedTypeSymbol AutoInjectExtensionsAttributeSymbol { get; } = compilation.GetTypeByMetadataName(Constants.AutoInjectExtensionsAttributeFullName)!;
    public INamedTypeSymbol AutoInjectModuleAttributeSymbol { get; } = compilation.GetTypeByMetadataName(Constants.AutoInjectModuleAttributeFullName)!;
    public INamedTypeSymbol? AutoOptionsAttributeSymbol { get; } = compilation.GetTypeByMetadataName(Constants.AutoOptionsAttributeFullName);
    public INamedTypeSymbol AutoInjectAttributeSymbol { get; } = compilation.GetTypeByMetadataName(Constants.AutoInjectAttributeFullName)!;

    public INamedTypeSymbol SingletonServiceAttributeSymbol { get; } = compilation.GetTypeByMetadataName(Constants.SingletonServiceAttributeFullName)!;
    public INamedTypeSymbol ScopedServiceAttributeSymbol { get; } = compilation.GetTypeByMetadataName(Constants.ScopedServiceAttributeFullName)!;
    public INamedTypeSymbol TransientServiceAttributeSymbol { get; } = compilation.GetTypeByMetadataName(Constants.TransientServiceAttributeFullName)!;

    public bool IsAutoInjectAttribute(INamedTypeSymbol? symbol)
    {
        return symbol is not null
            && (SymbolEqualityComparer.Default.Equals(symbol, SingletonServiceAttributeSymbol)
            || SymbolEqualityComparer.Default.Equals(symbol, ScopedServiceAttributeSymbol)
            || SymbolEqualityComparer.Default.Equals(symbol, TransientServiceAttributeSymbol)
            || SymbolEqualityComparer.Default.Equals(symbol, AutoInjectAttributeSymbol));
    }

    public bool IsAutoOptionsAttribute(INamedTypeSymbol? symbol)
    {
        return symbol is not null
            && symbol.ToDisplayString() == Constants.AutoOptionsAttributeFullName;
    }

    public string? GetLifetime(INamedTypeSymbol? symbol)
    {
        if (SymbolEqualityComparer.Default.Equals(symbol, SingletonServiceAttributeSymbol))
        {
            return "Singleton";
        }
        else if (SymbolEqualityComparer.Default.Equals(symbol, ScopedServiceAttributeSymbol))
        {
            return "Scoped";
        }
        else if (SymbolEqualityComparer.Default.Equals(symbol, TransientServiceAttributeSymbol))
        {
            return "Transient";
        }

        return null;
    }
}
