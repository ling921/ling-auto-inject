using Ling.AutoInject.SourceGenerators.Analyzers;
using Ling.AutoInject.SourceGenerators.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Ling.AutoInject.SourceGenerators.Tests.Verifiers.CSharpAnalyzerVerifier<
    Ling.AutoInject.SourceGenerators.Analyzers.AutoInjectAttributeAnalyzer>;

namespace Ling.AutoInject.SourceGenerators.Tests.Analyzers;

/// <summary>
/// Tests for registrations that cannot be represented by generated generic DI APIs.
/// </summary>
public sealed class UnsupportedRegistrationTargetAnalyzerTests
{
    [Theory]
    [InlineData("abstract", "public abstract class MyService")]
    [InlineData("static", "public static class MyService")]
    [InlineData("generic", "public class MyService<T>")]
    public async Task UnsupportedRegistrationTarget_ReportsDiagnostic(string kind, string declaration)
    {
        var source = $$"""
            using Ling.AutoInject;

            namespace Test
            {
                [ScopedService]
                {{declaration}}
                {
                }
            }
            """;

        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.UnsupportedRegistrationTargetRule)
            .WithLocation(5, 6)
            .WithArguments(kind, "MyService");

        await VerifyCS.VerifyAnalyzerAsync(source, diagnostic);
    }

    [Fact]
    public async Task RecordClass_IsSupported()
    {
        const string source = """
            using Ling.AutoInject;

            namespace Test;

            [ScopedService]
            public sealed record MyService;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }
}
