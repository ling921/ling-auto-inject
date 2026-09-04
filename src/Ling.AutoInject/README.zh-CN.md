# Ling.AutoInject

[项目文档](https://github.com/ling921/ling-auto-inject#readme) | [English](README.md)

`Ling.AutoInject` 提供基于属性的依赖注入注册辅助功能，并集成源生成器，在编译期生成 `IServiceCollection` 扩展方法，自动注册被属性标记的服务。

## 特性

- 使用统一的 `AutoInject`，或 `SingletonService`、`ScopedService` 和 `TransientService` 声明服务。
- 支持显式服务类型和 keyed service 注册。
- 支持通过程序集级 `AutoInjectConfig` 配置生成的方法名、宿主类名和命名空间。
- 支持设置 `Replace = true` 替换已有服务注册。
- 支持 `Add`、`TryAdd`、`Replace`、`TryAddEnumerable` 注册策略，以及声明式注册全部已实现接口。
- 支持通过 `AutoInjectExtensionsAttribute` 自定义生成类，并选择是否包含 `IConfiguration` 参数。
- 提供 Roslyn 分析器，帮助发现常见错误和无效配置。

## 安装

```shell
dotnet add package Ling.AutoInject
```

或：

```powershell
Install-Package Ling.AutoInject
```

## 使用方式

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

不指定服务类型时，类型会注册为自身；指定服务类型时，会生成服务接口到实现类型的注册映射。

## 配置生成代码

```csharp
[assembly: Ling.AutoInject.AutoInjectConfig(
    MethodName = "AddCustomServices",
    ClassName = "ServiceExtensions",
    Namespace = "MyNamespace")]
```

这会生成 `MyNamespace.ServiceExtensions.AddCustomServices` 扩展方法。

## 替换已有注册

```csharp
[SingletonService(typeof(IFoo), Replace = true)]
public class MyService : IFoo { }
```

这会生成 `services.Replace(ServiceDescriptor.Singleton<IFoo, MyService>())`，而不是 `TryAddSingleton`。

## 统一属性、接口注册与策略

```csharp
using Microsoft.Extensions.DependencyInjection;

[AutoInject(ServiceLifetime.Scoped, RegisterImplementedInterfaces = true)]
public class FooService : IFoo, IBar { }

[AutoInject(ServiceLifetime.Singleton, typeof(ICache), Strategy = ServiceRegistrationStrategy.Replace)]
public class MemoryCache : ICache { }
```

`AutoInject` 在一个属性中指定生命周期、服务类型和策略；既有生命周期属性仍兼容。`RegisterImplementedInterfaces = true` 会注册全部已实现接口。键控服务不能使用 `TryAddEnumerable`，分析器会报告该无效组合。

## 使用 AutoInjectExtensionsAttribute

```csharp
using Ling.AutoInject;

namespace MyNamespace
{
    [AutoInjectExtensions(MethodName = "AddCustomServices")]
    public static partial class ServiceExtensions { }
}
```

生成的 partial 类会使用标记类的名称和命名空间。

设置 `IncludeConfiguration = true` 后，生成的方法会接收 `IConfiguration`：

```csharp
[AutoInjectExtensions(MethodName = "AddCustomServices", IncludeConfiguration = true)]
public static partial class ServiceExtensions { }
```

你可以实现对应的 partial 方法，添加需要配置的自定义注册：

```csharp
static partial void AddAdditionalServices(
    IServiceCollection services,
    IConfiguration configuration);
```

## 诊断与限制

- Keyed service 需要 DI Abstractions 包支持对应 API。
- 抽象类、静态类和包含泛型参数的类型不会被注册。
- 示例位于仓库的 `samples/Ling.AutoInject.Sample` 目录。

## 协议

[MIT](https://github.com/ling921/ling-auto-inject/blob/master/LICENSE)
