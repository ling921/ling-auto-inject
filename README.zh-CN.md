# Ling.AutoInject

[English](README.md) | 简体中文

[![NuGet](https://img.shields.io/nuget/v/Ling.AutoInject.svg)](https://www.nuget.org/packages/Ling.AutoInject/)

`Ling.AutoInject` 是一个基于属性和 Roslyn 增量源生成器的编译期依赖注入工具。它会发现被标记的服务类型，并生成 `IServiceCollection` 扩展方法完成注册。

## 特性

- 使用 `SingletonService`、`ScopedService` 和 `TransientService` 声明服务生命周期。
- 编译期生成服务注册代码，不需要运行时扫描程序集。
- 支持显式指定服务类型和 keyed service 注册。
- 支持使用 `Replace = true` 替换已有服务注册。
- 可通过程序集级 `AutoInjectConfig` 配置生成方法名、类名和命名空间。
- 可通过 `AutoInjectExtensionsAttribute` 自定义生成类，并选择是否注入 `IConfiguration`。
- 内置 Roslyn 分析器，可在 IDE 中检查属性使用和配置错误。

## 安装

```shell
dotnet add package Ling.AutoInject
```

或使用 Visual Studio Package Manager Console：

```powershell
Install-Package Ling.AutoInject
```

## 基本用法

为实现类型添加生命周期属性：

```csharp
using Ling.AutoInject;

[SingletonService]
public class MyService { }

[ScopedService(typeof(IFoo))]
public class FooService : IFoo { }

[TransientService(ServiceKey = "k1")]
public class KeyedService { }
```

然后调用生成的扩展方法：

```csharp
services.Add[MyAssembly]Services();
```

如果未指定服务类型，类型会注册为自身；指定服务类型时，则会生成从服务接口到实现类型的注册映射。

## 配置生成代码

可以使用程序集级 `AutoInjectConfig` 修改生成的命名空间、类名和方法名：

```csharp
[assembly: Ling.AutoInject.AutoInjectConfig(
    MethodName = "AddCustomServices",
    ClassName = "ServiceExtensions",
    Namespace = "MyNamespace")]
```

之后调用：

```csharp
services.AddCustomServices();
```

## 高级用法

### 替换已有注册

设置 `Replace = true` 后，生成器会使用 `Replace` 替换已有注册，而不是使用 `TryAdd`：

```csharp
[SingletonService(typeof(IFoo), Replace = true)]
public class MyService : IFoo { }
```

### 使用 AutoInjectExtensionsAttribute

也可以将 `AutoInjectExtensionsAttribute` 添加到静态 partial 类上：

```csharp
using Ling.AutoInject;

namespace MyNamespace
{
    [AutoInjectExtensions(MethodName = "AddCustomServices")]
    public static partial class ServiceExtensions { }
}
```

生成的扩展类会使用该类的命名空间和名称。

### 注入 IConfiguration

设置 `IncludeConfiguration = true`，生成的方法会接收 `IConfiguration` 参数，并将其传递给可选的 partial 方法：

```csharp
[AutoInjectExtensions(MethodName = "AddCustomServices", IncludeConfiguration = true)]
public static partial class ServiceExtensions { }
```

```csharp
static partial void AddAdditionalServices(
    IServiceCollection services,
    IConfiguration configuration);
```

## 注意事项

- Keyed service 需要目标项目引用的 DI Abstractions 支持对应 API。
- 生成器会跳过抽象类、静态类和包含泛型参数的类型。
- 可运行的示例位于 `samples/Ling.AutoInject.Sample`。

## 开发

```shell
dotnet build
dotnet test
```

源生成器和分析器位于 `src/Ling.AutoInject.SourceGenerators`。

## 贡献

欢迎提交 Issue 和 Pull Request。行为变更请同时补充测试。

## 协议

[MIT](LICENSE)
