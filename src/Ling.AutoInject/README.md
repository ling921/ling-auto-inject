# Ling.AutoInject

[English](README.md) | [简体中文](README.zh-CN.md)

`Ling.AutoInject` provides attribute-driven registration helpers and integrates with a source generator to emit `IServiceCollection` extension methods that register services discovered via attributes at compile time.

## Features

- Attributes: unified `AutoInject`, plus `SingletonService`, `ScopedService`, and `TransientService` for simple, declarative registration.
- Optional service typing and keyed registration support.
- Configurable generated method name, host class and namespace through an assembly-level `AutoInjectConfig` attribute.
- Service replacement: use `Replace = true` to replace existing registrations instead of skipping when a service is already registered.
- Registration strategies: `Add`, `TryAdd`, `Replace`, and `TryAddEnumerable`; optionally register every implemented interface.
- Class-level customization via `AutoInjectExtensionsAttribute` for control over method generation behavior, including optional `IConfiguration` parameter support.
- Complementary analyzers to surface common mistakes and invalid configurations in the IDE.

## Installation

via .NET CLI:
```
dotnet add package Ling.AutoInject
```

via Package Manager Console:
```
Install-Package Ling.AutoInject
```

## Usage

1. Decorate implementation types:

    ```csharp
    using Ling.AutoInject;

    [SingletonService]
    public class MyService { }

    [ScopedService(typeof(IFoo))]
    public class MyService : IFoo { }

    [TransientService(ServiceKey = "k1")]
    public class MyService { }
    ```

2. (Optional) Configure generator output naming:

    ```csharp
    [assembly: Ling.AutoInject.AutoInjectConfig(
        MethodName = "AddCustomServices",
        ClassName = "ServiceExtensions",
        Namespace = "MyNamespace")]
    ```

3. Call the generated extension in `Program` / `Startup`:

    ```csharp
    services.Add[MyAssembly]Services();
    // or
    services.AddCustomServices();
    ```

## Notes

- If no service type is specified, the implementation type is registered as itself.
- If a service type is specified, the generator registers the mapping from service interface to implementation.
- Keyed registration requires the DI Abstractions package to support keyed APIs; analyzers warn when unsupported.
- A runnable record-type registration example is available in the repository under `samples/Ling.AutoInject.Sample`.
- The analyzer host requires Roslyn 4.3+ (Visual Studio 2022 17.3+ or .NET SDK 6.0.4xx+), independently of the application's target framework.

## Advanced features

### Unified registration attribute

`AutoInject` combines lifetime, service type, and strategy in one attribute. The existing lifetime-specific attributes remain supported.

```csharp
using Microsoft.Extensions.DependencyInjection;

[AutoInject(ServiceLifetime.Scoped, RegisterImplementedInterfaces = true)]
public class FooService : IFoo, IBar { }

[AutoInject(ServiceLifetime.Singleton, typeof(ICache), Strategy = ServiceRegistrationStrategy.Replace)]
public class MemoryCache : ICache { }
```

`RegisterImplementedInterfaces = true` registers all interfaces, including inherited interfaces, without registering the class itself. Each interface maps directly to the implementation and has its own lifetime cache. This prevents an existing or replaced interface registration from redirecting another interface to the wrong implementation. To share a scoped/singleton instance with the default strategy, add a separate self-registration attribute.

`Strategy` defaults to `TryAdd` on both unified and lifetime-specific attributes:

| Strategy | Behavior |
| --- | --- |
| `Add` | Appends a registration each time the generated method runs. |
| `TryAdd` | Preserves an existing registration for the same service type and key. |
| `Replace` | Removes the first registration with the same service type and key, then appends the new one. |
| `TryAddEnumerable` | Adds each distinct implementation once for the same service type and key. Self-registration uses equivalent `TryAdd` semantics. Interface registrations retain separate instances even when a self-registration is present. |

Keyed registrations support all four strategies with DI Abstractions 8.0+. A null key means unkeyed registration; array keys are rejected because they use reference equality. On older DI versions, keys are ignored with `LAI101`. The new `Strategy = Replace` works for unkeyed services on every supported DI version. The legacy `Replace = true` property takes precedence over `Strategy` and keeps its existing pre-8 warning/fallback behavior.

Invalid enum values and empty interface registration sets produce `LAI009`. Named `ServiceType` on `AutoInject` overrides its constructor service type. Existing lifetime attributes and generated entry points remain available. Compared with 1.2, interface-only registrations no longer implicitly share instances by forwarding through the first registered interface.

### Replace existing registrations

Use the `Replace` property to replace existing service registrations instead of using `TryAdd` methods:

```csharp
[SingletonService(typeof(IFoo), Replace = true)]
public class MyService : IFoo { }
```

This generates `services.Replace(ServiceDescriptor.Singleton<IFoo, MyService>())` instead of `services.TryAddSingleton<IFoo, MyService>()`.

### Using AutoInjectExtensionsAttribute

Instead of using the assembly-level `AutoInjectConfig`, you can decorate a static partial class with `AutoInjectExtensionsAttribute` for more control:

```csharp
using Ling.AutoInject;

namespace MyNamespace
{
    [AutoInjectExtensions(MethodName = "AddCustomServices")]
    public static partial class MyServiceExtensions { }
}
```

This generates a partial class with the specified method name in the same namespace as the decorated class.

#### Including IConfiguration

Use `IncludeConfiguration = true` to generate a method that accepts an `IConfiguration` parameter:

```csharp
[AutoInjectExtensions(MethodName = "AddCustomServices", IncludeConfiguration = true)]
public static partial class MyServiceExtensions { }
```

This generates:

```csharp
public static IServiceCollection AddCustomServices(this IServiceCollection services, IConfiguration configuration)
{
    // ...
    AddAdditionalServices(services, configuration);
    return services;
}

static partial void AddAdditionalServices(IServiceCollection services, IConfiguration configuration);
```

You can implement the `AddAdditionalServices` partial method to add custom service registrations that require configuration.

## Roadmap

### Version 2.0 — .NET-aligned lifecycle

Version 2.0 closes the standalone 1.x line. Future compatibility policy, supported compiler hosts, and release cadence will follow the support lifecycle of the corresponding .NET releases. New DI capabilities will be evaluated against that lifecycle and community feedback; existing attributes and generated entry points remain compatibility priorities.

## Contributing

- PRs and issues welcome. Include tests for analyzer/generator changes.

## License

- MIT License.
