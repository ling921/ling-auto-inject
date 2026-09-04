#if NET8_0_OR_GREATER
using Ling.AutoInject;
using Microsoft.Extensions.DependencyInjection;

namespace Ling.AutoInject.SourceGenerators.Tests;

/// <summary>
/// Exercises generated registrations against the Microsoft DI container at runtime.
/// </summary>
public sealed class RuntimeRegistrationTests
{
    [Fact]
    public void GeneratedRegistration_RegistersRecordServiceWithScopedLifetime()
    {
        var services = new ServiceCollection();
        services.AddRuntimeServices();

        using var provider = services.BuildServiceProvider();
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        var first = firstScope.ServiceProvider.GetRequiredService<IRuntimeGreetingService>();
        var secondFromSameScope = firstScope.ServiceProvider.GetRequiredService<IRuntimeGreetingService>();
        var secondFromDifferentScope = secondScope.ServiceProvider.GetRequiredService<IRuntimeGreetingService>();

        Assert.Equal("Hello, AutoInject!", first.Greet("AutoInject"));
        Assert.Same(first, secondFromSameScope);
        Assert.NotSame(first, secondFromDifferentScope);
    }
}

[AutoInjectExtensions(MethodName = "AddRuntimeServices")]
public static partial class RuntimeServiceCollectionExtensions
{
}

public interface IRuntimeGreetingService
{
    string Greet(string name);
}

[ScopedService(typeof(IRuntimeGreetingService))]
public sealed record RuntimeGreetingService : IRuntimeGreetingService
{
    public string Greet(string name) => $"Hello, {name}!";
}
#endif
