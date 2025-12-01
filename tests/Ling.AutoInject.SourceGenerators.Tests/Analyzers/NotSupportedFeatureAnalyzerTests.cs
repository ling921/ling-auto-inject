using Ling.AutoInject.SourceGenerators.Analyzers;
using Ling.AutoInject.SourceGenerators.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Ling.AutoInject.SourceGenerators.Tests.Verifiers.CSharpAnalyzerVerifier<
    Ling.AutoInject.SourceGenerators.Analyzers.NotSupportedFeatureAnalyzer>;

namespace Ling.AutoInject.SourceGenerators.Tests.Analyzers;

/// <summary>
/// Tests for <see cref="NotSupportedFeatureAnalyzer"/>.
/// </summary>
public sealed class NotSupportedFeatureAnalyzerTests
{
    [Theory]
    [InlineData("Singleton")]
    [InlineData("Scoped")]
    [InlineData("Transient")]
    public async Task NoUnsupportedFeature_NoDiagnostic(string lifetime)
    {
        var source = $$"""
            using Ling.AutoInject;

            namespace Test
            {
                [{{lifetime}}Service]
                public class MyService
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Theory]
    [InlineData("Singleton")]
    [InlineData("Scoped")]
    [InlineData("Transient")]
    public async Task KeyedService_OnUnsupportedVersion_ReportsDiagnostic(string lifetime)
    {
        var source = $$"""
            using Ling.AutoInject;

            namespace Test
            {
                [{{lifetime}}Service(ServiceKey = "k1")]
                public class MyService
                {
                }
            }
            """;

#if NET8_0_OR_GREATER
        await VerifyCS.VerifyAnalyzerAsync(source);
#else
        var dr = new DiagnosticResult(DiagnosticDescriptors.NotSupportedKeyedServiceRule)
            .WithLocation(5, lifetime.Length + 14);
        await VerifyCS.VerifyAnalyzerAsync(source, dr);
#endif
    }

    [Theory]
    [InlineData("Singleton")]
    [InlineData("Scoped")]
    [InlineData("Transient")]
    public async Task ReplaceService_OnUnsupportedVersion_ReportsDiagnostic(string lifetime)
    {
        var source = $$"""
            using Ling.AutoInject;

            namespace Test
            {
                public interface IFoo { }

                [{{lifetime}}Service(typeof(IFoo), Replace = true)]
                public class MyService : IFoo
                {
                }
            }
            """;

#if NET8_0_OR_GREATER
        await VerifyCS.VerifyAnalyzerAsync(source);
#else
        var dr = new DiagnosticResult(DiagnosticDescriptors.NotSupportedReplaceServiceRule)
            .WithLocation(7, lifetime.Length + 28);
        await VerifyCS.VerifyAnalyzerAsync(source, dr);
#endif
    }

#if NET8_0_OR_GREATER

    [Theory]
    [InlineData("Singleton")]
    [InlineData("Scoped")]
    [InlineData("Transient")]
    public async Task ReplaceTrue_NoServiceType_ReportsDiagnostic(string lifetime)
    {
        var source = $$"""
            using Ling.AutoInject;

            namespace Test
            {
                public interface IFoo { }

                [{{lifetime}}Service(Replace = true)]
                public class MyService : IFoo
                {
                }
            }
            """;

        var dr = new DiagnosticResult(DiagnosticDescriptors.RequiredServiceTypeForReplaceRule)
            .WithLocation(7, lifetime.Length + 14);
        await VerifyCS.VerifyAnalyzerAsync(source, dr);
    }

    [Theory]
    [InlineData("Singleton")]
    [InlineData("Scoped")]
    [InlineData("Transient")]
    public async Task ReplaceFalse_NoServiceType_NoDiagnostic(string lifetime)
    {
        var source = $$"""
            using Ling.AutoInject;

            namespace Test
            {
                public interface IFoo { }

                [{{lifetime}}Service(Replace = false)]
                public class MyService : IFoo
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

#endif
}
