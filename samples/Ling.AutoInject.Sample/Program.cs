using Ling.AutoInject;
using Ling.AutoInject.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

var services = new ServiceCollection();
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .Build();
services.AddSampleServices(configuration);

using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();

var greetingService = scope.ServiceProvider.GetRequiredService<IGreetingService>();
Console.WriteLine(greetingService.Greet("AutoInject"));

var greetingOptions = provider.GetRequiredService<IOptions<GreetingOptions>>().Value;
Console.WriteLine($"Configured prefix: {greetingOptions.Prefix}");

[AutoInjectExtensions(MethodName = "AddSampleServices")]
public static partial class SampleServiceCollectionExtensions
{
}

[AutoInjectModule]
public static partial class GreetingModule
{
}

public interface IGreetingService
{
    string Greet(string name);
}

[ScopedService(typeof(IGreetingService))]
public sealed record GreetingService(IOptions<GreetingOptions> options) : IGreetingService
{
    public string Greet(string name) => $"{options.Value.Prefix}, {name}!";
}

[AutoOptions("Greeting", ValidateDataAnnotations = true, ValidateOnStart = true, Module = typeof(GreetingModule))]
public sealed class GreetingOptions
{
    [Required]
    public string Prefix { get; set; } = string.Empty;
}
