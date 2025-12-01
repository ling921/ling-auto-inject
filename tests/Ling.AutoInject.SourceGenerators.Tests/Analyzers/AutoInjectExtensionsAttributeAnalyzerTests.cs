using Ling.AutoInject.SourceGenerators.Analyzers;
using Ling.AutoInject.SourceGenerators.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Ling.AutoInject.SourceGenerators.Tests.Verifiers.CSharpAnalyzerVerifier<
    Ling.AutoInject.SourceGenerators.Analyzers.AutoInjectExtensionsAttributeAnalyzer>;

namespace Ling.AutoInject.SourceGenerators.Tests.Analyzers;

/// <summary>
/// Tests for <see cref="AutoInjectExtensionsAttributeAnalyzer"/>.
/// </summary>
public sealed class AutoInjectExtensionsAttributeAnalyzerTests
{
    [Theory]
    [InlineData("MethodName")]
    [InlineData("methodName")]
    [InlineData("Method_Name")]
    [InlineData("@MethodName")]
    [InlineData("_MethodName")]
    public async Task ValidMethodName_NoDiagnostic(string value)
    {
        var source = $$"""
            using Ling.AutoInject;
            
            namespace Test
            {
                [AutoInjectExtensions(MethodName = "{{value}}")]
                public static partial class ServiceExtensions
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Theory]
    [InlineData("Method-Name")]
    [InlineData("Method.Name")]
    [InlineData("Method@Name")]
    [InlineData("123MethodName")]
    public async Task InvalidMethodName_ReportsDiagnostic(string value)
    {
        var source = $$"""
            using Ling.AutoInject;
            
            namespace Test
            {
                [AutoInjectExtensions(MethodName = "{{value}}")]
                public static partial class ServiceExtensions
                {
                }
            }
            """;

        var dr = new DiagnosticResult(DiagnosticDescriptors.InvalidNamingRule)
            .WithLocation(5, 27)
            .WithArguments(value, "method name");
        await VerifyCS.VerifyAnalyzerAsync(source, dr);
    }

    [Fact]
    public async Task MultipleAttributeUsages_ReportsDiagnostic()
    {
        const string source = """
            using Ling.AutoInject;
            
            namespace Test
            {
                [AutoInjectExtensions]
                public static partial class ServiceExtensions
                {
                }

                [AutoInjectExtensions]
                public static partial class AnotherServiceExtensions
                {
                }
            }
            """;

        var dr = new DiagnosticResult(DiagnosticDescriptors.MultipleExtensionsUsedRule)
            .WithLocation(11, 33);
        await VerifyCS.VerifyAnalyzerAsync(source, dr);
    }

    [Fact]
    public async Task NonPartialClass_ReportsDiagnostic()
    {
        const string source = """
            using Ling.AutoInject;
            
            namespace Test
            {
                [AutoInjectExtensions]
                public static class ServiceExtensions
                {
                }
            }
            """;

        var dr = new DiagnosticResult(DiagnosticDescriptors.RequiredStaticPartialClassRule)
            .WithLocation(6, 25)
            .WithArguments("ServiceExtensions");
        await VerifyCS.VerifyAnalyzerAsync(source, dr);
    }

    [Fact]
    public async Task NonStaticClass_ReportsDiagnostic()
    {
        var source = """
            using Ling.AutoInject;
            
            namespace Test
            {
                [AutoInjectExtensions]
                public partial class ServiceExtensions
                {
                }
            }
            """;

        var dr = new DiagnosticResult(DiagnosticDescriptors.RequiredStaticPartialClassRule)
            .WithLocation(6, 26)
            .WithArguments("ServiceExtensions");
        await VerifyCS.VerifyAnalyzerAsync(source, dr);
    }
}
