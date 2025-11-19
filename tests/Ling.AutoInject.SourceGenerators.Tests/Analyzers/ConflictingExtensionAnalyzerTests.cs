using Ling.AutoInject.SourceGenerators.Analyzers;
using Ling.AutoInject.SourceGenerators.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Ling.AutoInject.SourceGenerators.Tests.Verifiers.CSharpAnalyzerVerifier<
    Ling.AutoInject.SourceGenerators.Analyzers.ConflictingExtensionAnalyzer>;

namespace Ling.AutoInject.SourceGenerators.Tests.Analyzers;

/// <summary>
/// Tests for <see cref="ConflictingExtensionAnalyzer"/>.
/// </summary>
public sealed class ConflictingExtensionAnalyzerTests
{
    [Fact]
    public async Task Analyze_NoConflictingExtension_NoDiagnostic()
    {
        var source = $$"""
            using Ling.AutoInject;
            using Microsoft.Extensions.DependencyInjection;

            [assembly: AutoInjectConfig(MethodName = "AddOtherServices")]

            namespace Test
            {
                public static class MyExtensions
                {
                    public static void AddMyServices(this IServiceCollection services)
                    {
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Analyze_ConflictingExtension_ByName_ReportsDiagnostic()
    {
        var source = $$"""
            using Ling.AutoInject;
            using Microsoft.Extensions.DependencyInjection;

            [assembly: AutoInjectConfig(MethodName = "AddMyServices")]

            namespace Test
            {
                public static class MyExtensions
                {
                    public static void AddMyServices(this IServiceCollection services)
                    {
                    }
                }
            }
            """;

        var dr = new DiagnosticResult(DiagnosticDescriptors.ConflictingExtensionRule)
            .WithLocation(4, 29)
            .WithArguments("AddMyServices");

        await VerifyCS.VerifyAnalyzerAsync(source, dr);
    }

    [Fact]
    public async Task Analyze_ConflictingExtension_WithNameof_ReportsDiagnostic()
    {
        var source = $$"""
            using Ling.AutoInject;
            using Microsoft.Extensions.DependencyInjection;

            [assembly: AutoInjectConfig(MethodName = nameof(Test.MyExtensions.AddMyServices))]

            namespace Test
            {
                public static class MyExtensions
                {
                    public static void AddMyServices(this IServiceCollection services)
                    {
                    }
                }
            }
            """;

        var dr = new DiagnosticResult(DiagnosticDescriptors.ConflictingExtensionRule)
            .WithLocation(4, 29)
            .WithArguments("AddMyServices");

        await VerifyCS.VerifyAnalyzerAsync(source, dr);
    }

    [Fact]
    public async Task Analyze_ConflictingExtension_WithConstExpr_ReportsDiagnostic()
    {
        var source = $$"""
            using Ling.AutoInject;
            using Microsoft.Extensions.DependencyInjection;

            [assembly: AutoInjectConfig(MethodName = Test.MyExtensions.MethodName))]

            namespace Test
            {
                public static class MyExtensions
                {
                    public const string MethodName = "AddMyServices";

                    public static void AddMyServices(this IServiceCollection services)
                    {
                    }
                }
            }
            """;

        var dr = new DiagnosticResult(DiagnosticDescriptors.ConflictingExtensionRule)
            .WithLocation(4, 29)
            .WithArguments("AddMyServices");

        await VerifyCS.VerifyAnalyzerAsync(source, dr);
    }
}
