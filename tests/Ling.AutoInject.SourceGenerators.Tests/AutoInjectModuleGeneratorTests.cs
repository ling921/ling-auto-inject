using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;

namespace Ling.AutoInject.SourceGenerators.Tests;

public sealed class AutoInjectModuleGeneratorTests
{
    [Fact]
    public void ModuleRegistration_IsGeneratedOnlyInItsModuleAndCalledByRoot()
    {
        const string source = """
            using Ling.AutoInject;

            namespace Test;

            [AutoInjectModule]
            public static partial class WebModule { }

            [SingletonService(Module = typeof(WebModule))]
            public sealed class WebService { }
            """;

        var compilation = CSharpCompilation.Create(
            "TestProject",
            [CSharpSyntaxTree.ParseText(source)],
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(IServiceCollection).Assembly.Location),
            ],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new AutoInjectGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

        var generated = driver.GetRunResult().Results.Single().GeneratedSources
            .ToDictionary(sourceResult => sourceResult.HintName, sourceResult => sourceResult.SourceText.ToString());
        var root = generated["AutoInject_TestProject.g.cs"];
        var module = generated.Single(pair => pair.Key.StartsWith("AutoInject_Module_", StringComparison.Ordinal)).Value;

        Assert.Contains("global::Test.WebModule.AddWebModule(services);", root);
        Assert.DoesNotContain("WebService", root);
        Assert.Contains("services.TryAddSingleton<global::Test.WebService>();", module);
    }

    [Fact]
    public void OptionsModule_GeneratesBindingAndValidation()
    {
        const string source = """
            namespace Ling.AutoInject.Options
            {
                [global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = true)]
                public sealed class AutoOptionsAttribute(string sectionPath) : global::System.Attribute
                {
                    public string? Name { get; set; }
                    public bool ValidateDataAnnotations { get; set; }
                    public bool ValidateOnStart { get; set; }
                    public global::System.Type? Module { get; set; }
                }
            }

            namespace Test
            {
                using Ling.AutoInject;
                using Ling.AutoInject.Options;

                [AutoInjectModule]
                public static partial class GreetingModule { }

                [AutoOptions("Greeting", Name = "primary", ValidateDataAnnotations = true, ValidateOnStart = true, Module = typeof(GreetingModule))]
                public sealed class GreetingOptions { }
            }
            """;

        var compilation = CSharpCompilation.Create(
            "TestProject",
            [CSharpSyntaxTree.ParseText(source)],
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(IServiceCollection).Assembly.Location),
            ],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new AutoInjectGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

        var generated = driver.GetRunResult().Results.Single().GeneratedSources
            .ToDictionary(sourceResult => sourceResult.HintName, sourceResult => sourceResult.SourceText.ToString());
        var root = generated["AutoInject_TestProject.g.cs"];
        var module = generated.Single(pair => pair.Key.StartsWith("AutoInject_Module_", StringComparison.Ordinal)).Value;

        Assert.Contains("global::Test.GreetingModule.AddGreetingModule(services, configuration);", root);
        Assert.Contains("services.AddOptions<global::Test.GreetingOptions>(\"primary\")", module);
        Assert.Contains(".Bind(configuration.GetSection(\"Greeting\"))", module);
        Assert.Contains("autoOptions0.ValidateDataAnnotations();", module);
        Assert.Contains("autoOptions0.ValidateOnStart();", module);
    }
}
