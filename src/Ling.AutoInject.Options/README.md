# Ling.AutoInject.Options

[English](README.md) | [简体中文](README.zh-CN.md)

Optional Options and configuration-binding contracts for `Ling.AutoInject`. Install this package to use `[AutoOptions]`; it brings the compatible core package and source generator.

```csharp
using Ling.AutoInject.Options;

[AutoOptions("Clients:Primary", Name = "primary", ValidateDataAnnotations = true)]
public sealed class ClientOptions { }
```

The generated registration accepts `IConfiguration` explicitly and uses `AddOptions<T>().Bind(configuration.GetSection(...))`.

`SectionPath` must consist of non-empty, trimmed segments separated by `:`. The same options type can be registered more than once only with different names; the analyzer reports duplicate type/name pairs. `ValidateOnStart` requires Microsoft.Extensions.Options 6.0 or later.

Use `Module = typeof(SomeModule)` to make an Options registration part of a generated `[AutoInjectModule]` entry point.
