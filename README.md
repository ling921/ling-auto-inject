# Ling.AutoInject [![NuGet](https://img.shields.io/nuget/v/Ling.AutoInject.svg)](https://www.nuget.org/packages/Ling.AutoInject/)

`Ling.AutoInject` provides attribute-driven dependency injection registrations plus a source generator that emits `IServiceCollection` extension methods for automatic registration.

## Features
- Attribute-based registration: `SingletonService`, `ScopedService`, `TransientService`.
- Compile-time source generator that emits a single extension method to register discovered services.
- Roslyn analyzers that validate attribute usage and `AutoInjectConfig` values at design time.
- Optional keyed service support when the DI abstractions package supports it.
- Configurable generated method, class and namespace via an assembly-level `AutoInjectConfig` attribute.

## Quick start
### 1. Install the runtime package (NuGet):

via .NET CLI:
```
dotnet add package Ling.AutoInject
```

via Package Manager Console:
```
Install-Package Ling.AutoInject
```

### 2. Annotate implementation types:

```csharp
using Ling.AutoInject;

[SingletonService]
public class MyService { }

public interface IFoo { }

[ScopedService(typeof(IFoo))]
public class MyService : IFoo { }

[TransientService(ServiceKey = "k1")]
public class MyService { }
```

### 3. Optionally configure the generated extension method name, class and namespace:

```csharp
[assembly: Ling.AutoInject.AutoInjectConfig(
    MethodName = "AddCustomServices",
    ClassName = "ServiceExtensions",
    Namespace = "MyNamespace")]
```

### 4. Call the generated extension in `Program` or `Startup`:

```csharp
services.Add[MyAssembly]Services(); // default generated name
// or
services.AddCustomServices(); // when configured via AutoInjectConfig
```

Notes
- When an attribute omits a service type, the implementation type itself is registered.
- When an attribute specifies a service type, the generator registers the service interface to the implementation.
- Keyed service APIs require `Microsoft.Extensions.DependencyInjection.Abstractions` 8.0.0 or later; the analyzers will warn when keyed registration is used but the target package does not support it.
- The repository includes analyzers that report duplicates, lifetime conflicts and invalid configuration values in the IDE.

## Development
- Build: `dotnet build`
- Test: `dotnet test`
- Source generator and analyzers live under `src/Ling.AutoInject.SourceGenerators`.

## Planned enhancements
- Attribute-driven Options registration: upcoming support will generate registrations and configuration binding for `IOptions<T>` style options classes.

## Contributing
- Contributions are welcome. Please open issues or PRs and include tests for new behavior.

## License

- [MIT License](LICENSE)
