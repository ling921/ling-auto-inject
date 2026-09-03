# Ling.AutoInject

`Ling.AutoInject` provides attribute-driven registration helpers and integrates with a source generator to emit `IServiceCollection` extension methods that register services discovered via attributes at compile time.

## Features

- Attributes: `SingletonService`, `ScopedService`, `TransientService` for simple, declarative registration.
- Optional service typing and keyed registration support.
- Configurable generated method name, host class and namespace through an assembly-level `AutoInjectConfig` attribute.
- Service replacement: use `Replace = true` to replace existing registrations instead of skipping when a service is already registered.
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

## Advanced features

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

The project will continue to evolve around three priorities: generator correctness, expressive registration capabilities, and integration with the broader .NET dependency injection ecosystem.

### Version 1.2 — Correctness and compatibility

- Unify source generator and analyzer behavior for framework and package version detection.
- Improve diagnostics for unsupported registration targets, including abstract, static, and generic types.
- Extend discovery to support record class types and extension hosts declared in the global namespace.
- Strengthen validation for duplicate registrations, conflicting lifetimes, service type mismatches, and configuration conflicts.
- Add runtime integration tests in addition to generated-source snapshot tests.

### Version 1.3 — Registration capabilities

- Introduce an optional unified `AutoInject` attribute while preserving the existing lifetime-specific attributes.
- Support registration of all implemented interfaces through declarative configuration.
- Add explicit registration strategies, including `Add`, `TryAdd`, `Replace`, and `TryAddEnumerable`.
- Improve keyed service support with stronger key validation and more consistent registration semantics.

### Version 1.4 — Configuration and modularization

- Add attribute-driven Options registration and configuration binding for `IOptions<T>`-style options classes.
- Support configuration validation, including startup validation and data annotation validation where applicable.
- Support module-level registration methods for large applications and multi-module solutions.
- Allow multiple generated registration entry points where project organization requires them.

### Version 2.0 — Advanced dependency injection scenarios

- Support open generic registrations with appropriate compile-time validation.
- Support factory, delegate, and instance registrations.
- Provide decorator and interception-oriented registration capabilities.
- Add conditional registrations based on configuration or environment.
- Explore dependency graph diagnostics and visualization for generated registrations.

The roadmap is subject to change based on compatibility requirements, community feedback, and the evolution of the Microsoft.Extensions.DependencyInjection APIs. Backward compatibility with existing attributes and generated registration entry points will remain a primary consideration.

## Contributing

- PRs and issues welcome. Include tests for analyzer/generator changes.

## License

- MIT License.
