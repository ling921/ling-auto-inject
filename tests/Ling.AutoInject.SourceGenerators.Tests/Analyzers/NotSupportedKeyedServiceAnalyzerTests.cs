using Ling.AutoInject.SourceGenerators.Analyzers;
using Ling.AutoInject.SourceGenerators.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Ling.AutoInject.SourceGenerators.Tests.Verifiers.CSharpAnalyzerVerifier<
    Ling.AutoInject.SourceGenerators.Analyzers.NotSupportedKeyedServiceAnalyzer>;

namespace Ling.AutoInject.SourceGenerators.Tests.Analyzers;

/// <summary>
/// Tests for <see cref="NotSupportedKeyedServiceAnalyzer"/>.
/// </summary>
public sealed class NotSupportedKeyedServiceAnalyzerTests
{
    [Theory]
    [InlineData("Singleton")]
    [InlineData("Scoped")]
    [InlineData("Transient")]
    public async Task NotSupportedKeyedService_NoKeyedService_ReportsNoDiagnostic(string lifetime)
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
    public async Task NotSupportedKeyedService_WithKeyedService_OnUnsupportedVersion_ReportsDiagnostic(string lifetime)
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
}
