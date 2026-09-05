# Ling.AutoInject [![NuGet](https://img.shields.io/nuget/v/Ling.AutoInject.svg)](https://www.nuget.org/packages/Ling.AutoInject/)

[English](README.md) | [简体中文](README.zh-CN.md)

`Ling.AutoInject` provides attribute-driven dependency injection registrations plus a source generator that emits `IServiceCollection` extension methods for automatic registration.

## Features
- Attribute-based registration: `AutoInject`, `SingletonService`, `ScopedService`, `TransientService`.
- Compile-time source generator that emits a single extension method to register discovered services.
- Roslyn analyzers that validate attribute usage and `AutoInjectConfig` values at design time.
- Optional keyed service support when the DI abstractions package supports it.
- Configurable generated method, class and namespace via an assembly-level `AutoInjectConfig` attribute.
- Service replacement support: use `Replace = true` to replace existing registrations instead of skipping when a service is already registered.
- Explicit `Add`, `TryAdd`, `Replace`, and `TryAddEnumerable` strategies, plus declarative registration of all implemented interfaces.
- Class-level customization via `AutoInjectExtensionsAttribute` for control over method generation behavior, including optional `IConfiguration` parameter support.

## Usage

For detailed usage instructions, including installation, attribute-based registration, and more, see the [package README](src/Ling.AutoInject/README.md).

Version 1.3 adds `[AutoInject(ServiceLifetime.Scoped, RegisterImplementedInterfaces = true)]`. Interfaces are registered directly with independent lifetime caches; all four strategies support keyed services on DI 8.0+. See the package README for instance-sharing and compatibility details.

## Development
- Build: `dotnet build`
- Test: `dotnet test`
- Source generator and analyzers live under `src/Ling.AutoInject.SourceGenerators`.
- Executable sample: `samples/Ling.AutoInject.Sample` demonstrates record-type discovery and generated registration against the Microsoft DI container.

## Roadmap

The project will continue to evolve around three priorities: generator correctness, expressive registration capabilities, and integration with the broader .NET dependency injection ecosystem.

### Version 1.2 — Correctness and compatibility

- Unify source generator and analyzer behavior for framework and package version detection.
- Improve diagnostics for unsupported registration targets, including abstract, static, and generic types.
- Extend discovery to support record class types and extension hosts declared in the global namespace.
- Strengthen validation for duplicate registrations, conflicting lifetimes, service type mismatches, and configuration conflicts.
- Add runtime integration tests in addition to generated-source snapshot tests.

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
- Contributions are welcome. Please open issues or PRs and include tests for new behavior.

## License

- [MIT License](LICENSE)
