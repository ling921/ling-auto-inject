# Ling.AutoInject.Options

[English](README.md) | [简体中文](README.zh-CN.md)

`Ling.AutoInject` 的可选 Options 与配置绑定契约包。安装此包即可使用 `[AutoOptions]`；它会带入兼容的核心包和源生成器：

```csharp
using Ling.AutoInject.Options;

[AutoOptions("Clients:Primary", Name = "primary", ValidateDataAnnotations = true)]
public sealed class ClientOptions { }
```

生成代码显式接收 `IConfiguration`，并使用 `AddOptions<T>().Bind(configuration.GetSection(...))`。

`SectionPath` 必须由非空且不含首尾空白的节通过 `:` 连接；同一个 Options 类型仅可用不同名称注册多次，重复的“类型 + 名称”会由分析器报错。`ValidateOnStart` 需要 Microsoft.Extensions.Options 6.0 或更高版本。

设置 `Module = typeof(SomeModule)` 可将 Options 注册归入生成的 `[AutoInjectModule]` 入口。
