# Ling.AutoInject [![NuGet](https://img.shields.io/nuget/v/Ling.AutoInject.svg)](https://www.nuget.org/packages/Ling.AutoInject/)

[English](README.md) | [简体中文](README.zh-CN.md)

`Ling.AutoInject` provides attribute-driven dependency injection registrations plus a source generator that emits `IServiceCollection` extension methods for automatic registration.

## Packages

| Package | Purpose |
| --- | --- |
| [`Ling.AutoInject`](src/Ling.AutoInject/README.md) | Core DI attributes, source generator, analyzers, and generated registration entry point. |
| [`Ling.AutoInject.Options`](src/Ling.AutoInject.Options/README.md) | Optional `[AutoOptions]` contract plus configuration binding and validation dependencies. |

## Features
- Attribute-based registration: `AutoInject`, `SingletonService`, `ScopedService`, `TransientService`.
- Compile-time source generator that emits a single extension method to register discovered services.
- Roslyn analyzers that validate attribute usage and `AutoInjectConfig` values at design time.
- Optional keyed service support when the DI abstractions package supports it.
- Configurable generated method, class and namespace via an assembly-level `AutoInjectConfig` attribute.
- Service replacement support: use `Replace = true` to replace existing registrations instead of skipping when a service is already registered.
- Explicit `Add`, `TryAdd`, `Replace`, and `TryAddEnumerable` strategies, plus declarative registration of all implemented interfaces.
- Class-level customization via `AutoInjectExtensionsAttribute` for control over method generation behavior, including optional `IConfiguration` parameter support.
- Optional `Ling.AutoInject.Options` package for attribute-driven Options binding, validation, and named options.

## Usage

For detailed usage instructions, including installation, attribute-based registration, and more, see the [package README](src/Ling.AutoInject/README.md).

Version 1.3 adds `[AutoInject(ServiceLifetime.Scoped, RegisterImplementedInterfaces = true)]`. Interfaces are registered directly with independent lifetime caches; all four strategies support keyed services on DI 8.0+. See the package README for instance-sharing and compatibility details.

## Options

Install `Ling.AutoInject.Options` to register configuration-bound options without adding configuration dependencies to DI-only projects:

```csharp
using Ling.AutoInject.Options;

[AutoOptions("Clients:Primary", Name = "primary", ValidateDataAnnotations = true)]
public sealed class ClientOptions { }
```

Generated entry points that contain Options accept `IConfiguration` and bind with `AddOptions<T>().Bind(configuration.GetSection(...))`. Section paths and duplicate type/name registrations are validated by analyzers.

### Modules

Use a top-level `static partial` class to group registrations into an independently callable entry point. The aggregate entry point calls every module once, so applications can use either the aggregate method or selected modules.

```csharp
[AutoInjectModule(MethodName = "AddWebModule")]
public static partial class WebModule { }

[ScopedService(typeof(IEndpoint), Module = typeof(WebModule))]
public sealed class Endpoint : IEndpoint { }
```

`Module` is also available on `[AutoOptions]`. A module that owns Options accepts `IConfiguration`; a DI-only module does not. The analyzer rejects non-module, nested, generic, non-static, or non-partial module targets.

## Development
- Build: `dotnet build`
- Test: `dotnet test`
- Source generator and analyzers live under `src/Ling.AutoInject.SourceGenerators`.
- Executable sample: `samples/Ling.AutoInject.Sample` demonstrates record-type discovery and generated registration against the Microsoft DI container.
- The analyzer host requires Roslyn 4.3+ (Visual Studio 2022 17.3+ or .NET SDK 6.0.4xx+). This is independent of the target frameworks exposed by the package.

## Roadmap

### Version 2.0 — .NET-aligned lifecycle

Version 2.0 closes the standalone 1.x line. Future compatibility policy, supported compiler hosts, and release cadence will follow the support lifecycle of the corresponding .NET releases. New DI capabilities will be evaluated against that lifecycle and community feedback; existing attributes and generated entry points remain compatibility priorities.

## Contributing
- Contributions are welcome. Please open issues or PRs and include tests for new behavior.

## License

- [MIT License](LICENSE)
