using Ling.AutoInject;
using Ling.AutoInject.Options;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddNativeAotSmokeServices();

using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();

var greeter = scope.ServiceProvider.GetRequiredService<IGreeter>();
var counter = provider.GetRequiredKeyedService<ICounter>("main");
var moduleService = provider.GetRequiredService<IModuleService>();
_ = typeof(AutoOptionsAttribute);

if (greeter.Greet("AOT") != "Hello, AOT"
    || counter.Next() != 1
    || moduleService.Ping() != "pong")
{
    return 1;
}

Console.WriteLine("Ling.AutoInject Native AOT smoke passed.");
return 0;

[AutoInjectExtensions(MethodName = "AddNativeAotSmokeServices")]
public static partial class ServiceCollectionExtensions;

[AutoInjectModule]
public static partial class SmokeModule;

public interface IGreeter
{
    string Greet(string name);
}

[ScopedService(typeof(IGreeter))]
public sealed class Greeter : IGreeter
{
    public string Greet(string name) => $"Hello, {name}";
}

public interface ICounter
{
    int Next();
}

[SingletonService(typeof(ICounter), ServiceKey = "main")]
public sealed class Counter : ICounter
{
    private int _value;
    public int Next() => ++_value;
}

public interface IModuleService
{
    string Ping();
}

[TransientService(RegisterImplementedInterfaces = true, Module = typeof(SmokeModule))]
public sealed class ModuleService : IModuleService
{
    public string Ping() => "pong";
}
