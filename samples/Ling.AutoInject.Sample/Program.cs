using Ling.AutoInject;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddSampleServices();

using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();

var greetingService = scope.ServiceProvider.GetRequiredService<IGreetingService>();
Console.WriteLine(greetingService.Greet("AutoInject"));

[AutoInjectExtensions(MethodName = "AddSampleServices")]
public static partial class SampleServiceCollectionExtensions
{
}

public interface IGreetingService
{
    string Greet(string name);
}

[ScopedService(typeof(IGreetingService))]
public sealed record GreetingService : IGreetingService
{
    public string Greet(string name) => $"Hello, {name}!";
}
