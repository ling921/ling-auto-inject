# Ling.AutoInject

`Ling.AutoInject` provides attribute-driven registration helpers and integrates with a source generator to emit `IServiceCollection` extension methods that register services discovered via attributes at compile time.

## Features

- Attributes: `SingletonService`, `ScopedService`, `TransientService` for simple, declarative registration.
- Optional service typing and keyed registration support.
- Configurable generated method name, host class and namespace through an assembly-level `AutoInjectConfig` attribute.
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

## Planned features

- Options binding: future work will add attribute-driven registration and configuration binding for options classes (`IOptions<T>`), including section binding.

## Contributing

- PRs and issues welcome. Include tests for analyzer/generator changes.

## License

- MIT License.
