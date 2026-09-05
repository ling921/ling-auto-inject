#if NET8_0_OR_GREATER
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Ling.AutoInject.SourceGenerators.Tests;

public sealed class RegistrationBehaviorTests
{
    public static IEnumerable<object[]> Strategies()
    {
        foreach (var lifetime in new[] { "Singleton", "Scoped", "Transient" })
        foreach (var strategy in new[] { "Add", "TryAdd", "Replace", "TryAddEnumerable" })
        foreach (var keyed in new[] { false, true })
            yield return new object[] { lifetime, strategy, keyed };
    }

    [Theory]
    [MemberData(nameof(Strategies))]
    public void Strategies_PreserveImplementationsAndLifetimes(string lifetime, string strategy, bool keyed)
    {
        var keyArgument = keyed ? ", ServiceKey = 42" : "";
        var assembly = Generate($$"""
            using Ling.AutoInject;
            using Microsoft.Extensions.DependencyInjection;
            public interface IFoo { }
            public interface IBar { }
            public class Existing : IFoo { }
            [AutoInject(ServiceLifetime.{{lifetime}}, RegisterImplementedInterfaces = true,
                Strategy = ServiceRegistrationStrategy.{{strategy}}{{keyArgument}})]
            public class First : IFoo, IBar { }
            [AutoInject(ServiceLifetime.{{lifetime}}, typeof(IFoo),
                Strategy = ServiceRegistrationStrategy.{{strategy}}{{keyArgument}})]
            public class Second : IFoo { }
            """);
        var foo = assembly.GetType("IFoo")!;
        var bar = assembly.GetType("IBar")!;
        var existing = Activator.CreateInstance(assembly.GetType("Existing")!)!;
        var services = new ServiceCollection();
        if (keyed)
        {
            services.AddKeyedSingleton(foo, 42, existing);
            services.AddKeyedSingleton(foo, 43, existing);
        }
        else services.AddSingleton(foo, existing);
        Register(assembly, services);
        Register(assembly, services);

        var expectedCount = strategy switch { "Add" => 5, "TryAddEnumerable" => 3, _ => 1 };
        Assert.Equal(expectedCount + (keyed ? 1 : 0), services.Count(d => d.ServiceType == foo));
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();
        using var otherScope = provider.CreateScope();
        object Resolve(IServiceProvider sp, Type type) => keyed
            ? sp.GetRequiredKeyedService(type, 42) : sp.GetRequiredService(type);
        if (keyed) Assert.Same(existing, scope.ServiceProvider.GetRequiredKeyedService(foo, 43));
        var resolvedFoo = Resolve(scope.ServiceProvider, foo);
        Assert.Equal(strategy == "TryAdd" ? "Existing" : "Second", resolvedFoo.GetType().Name);
        var first = Resolve(scope.ServiceProvider, bar);
        Assert.Equal("First", first.GetType().Name);
        if (lifetime == "Transient") Assert.NotSame(first, Resolve(scope.ServiceProvider, bar));
        else Assert.Same(first, Resolve(scope.ServiceProvider, bar));
        if (lifetime == "Singleton") Assert.Same(first, Resolve(otherScope.ServiceProvider, bar));
        else Assert.NotSame(first, Resolve(otherScope.ServiceProvider, bar));
        if (strategy == "TryAddEnumerable")
        {
            var items = keyed ? scope.ServiceProvider.GetKeyedServices(foo, 42) : scope.ServiceProvider.GetServices(foo);
            Assert.Equal(new[] { "Existing", "First", "Second" }, items.Select(x => x!.GetType().Name));
        }
    }

    [Fact]
    public void PartialTypes_AndNamedServiceType_AreHandledOnce()
    {
        var assembly = Generate("""
            using Ling.AutoInject;
            using Microsoft.Extensions.DependencyInjection;
            public interface IFoo { }
            public interface IBar { }
            [AutoInject(ServiceLifetime.Singleton, typeof(IFoo), ServiceType = typeof(IBar), Strategy = ServiceRegistrationStrategy.Add)]
            public partial class First : IFoo, IBar { }
            [System.Obsolete]
            public partial class First { }
            """);
        var services = new ServiceCollection();
        Register(assembly, services);
        Assert.Single(services);
        Assert.Equal("IBar", services[0].ServiceType.Name);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Enumerable_RecognizesExistingTypedFactory(bool keyed)
    {
        var assembly = Generate($$"""
            using Ling.AutoInject;
            using Microsoft.Extensions.DependencyInjection;
            public interface IFoo { }
            [AutoInject(ServiceLifetime.Singleton, typeof(IFoo), Strategy = ServiceRegistrationStrategy.TryAddEnumerable{{(keyed ? ", ServiceKey = 42" : "")}})]
            public class First : IFoo { }
            public static class Initial
            {
                public static void Configure(IServiceCollection services)
                {
                    services.Add(ServiceDescriptor.{{(keyed ? "KeyedSingleton<IFoo, First>(42, (sp, key) => new First())" : "Singleton<IFoo, First>(sp => new First())")}});
                }
            }
            """);
        var services = new ServiceCollection();
        assembly.GetType("Initial")!.GetMethod("Configure")!.Invoke(null, new object[] { services });
        Register(assembly, services);
        Assert.Single(services);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("1.5")]
    [InlineData("true")]
    [InlineData("typeof(string)")]
    [InlineData("ServiceLifetime.Scoped")]
    [InlineData("null")]
    public void ConstantKeys_AreIdempotent(string key)
    {
        var assembly = Generate($$"""
            using Ling.AutoInject;
            using Microsoft.Extensions.DependencyInjection;
            [AutoInject(ServiceLifetime.Singleton, ServiceKey = {{key}})]
            public class First { }
            """);
        var services = new ServiceCollection();
        Register(assembly, services);
        Register(assembly, services);
        Assert.Single(services);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EnumerableSelfRegistration_DoesNotThrow(bool keyed)
    {
        var assembly = Generate($$"""
            using Ling.AutoInject;
            using Microsoft.Extensions.DependencyInjection;
            [AutoInject(ServiceLifetime.Singleton, Strategy = ServiceRegistrationStrategy.TryAddEnumerable{{(keyed ? ", ServiceKey = \"key\"" : "")}})]
            public class First { }
            """);
        var services = new ServiceCollection();
        Register(assembly, services);
        Register(assembly, services);
        Assert.Single(services);
    }

    [Fact]
    public void ExplicitSelfRegistration_PreservesSharedScopedInstance()
    {
        var assembly = Generate("""
            using Ling.AutoInject;
            using Microsoft.Extensions.DependencyInjection;
            public interface IFoo { }
            public interface IBar { }
            [AutoInject(ServiceLifetime.Scoped)]
            [AutoInject(ServiceLifetime.Scoped, RegisterImplementedInterfaces = true)]
            public class First : IFoo, IBar { }
            """);
        var services = new ServiceCollection();
        Register(assembly, services);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        Assert.Same(scope.ServiceProvider.GetRequiredService(assembly.GetType("IFoo")!),
            scope.ServiceProvider.GetRequiredService(assembly.GetType("IBar")!));
    }

    private static Assembly Generate(string source)
    {
        var paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Concat(new[] { typeof(IServiceCollection).Assembly.Location, typeof(ServiceCollectionContainerBuilderExtensions).Assembly.Location })
            .Distinct();
        var compilation = CSharpCompilation.Create("RegistrationTest_" + Guid.NewGuid().ToString("N"),
            new[] { CSharpSyntaxTree.ParseText(source) }, paths.Select(p => MetadataReference.CreateFromFile(p)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new AutoInjectGenerator().AsSourceGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        foreach (var generated in driver.GetRunResult().GeneratedTrees)
            Assert.DoesNotContain("\n", generated.GetText().ToString().Replace("\r\n", ""));
        using var stream = new MemoryStream();
        var result = output.Emit(stream);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        return Assembly.Load(stream.ToArray());
    }

    private static void Register(Assembly assembly, IServiceCollection services)
    {
        var method = assembly.GetTypes().SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Single(m => m.ReturnType == typeof(IServiceCollection));
        method.Invoke(null, new object[] { services });
    }
}
#endif
