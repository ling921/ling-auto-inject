using Ling.AutoInject.SourceGenerators.Diagnostics;
using Verify = Ling.AutoInject.SourceGenerators.Tests.Verifiers.CSharpAnalyzerVerifier<
    Ling.AutoInject.SourceGenerators.Analyzers.AutoOptionsAnalyzer>;

namespace Ling.AutoInject.SourceGenerators.Tests.Analyzers;

public sealed class AutoOptionsAnalyzerTests
{
    [Theory]
    [InlineData("")]
    [InlineData("Section:")]
    [InlineData(":Section")]
    [InlineData("Section::Child")]
    [InlineData("Section: Child")]
    public async Task InvalidPath_ReportsDiagnostic(string path)
    {
        var source = $$"""
            using Ling.AutoInject.Options;
            [AutoOptions("{{path}}")]
            public sealed class Options { }
            """;
        await Verify.VerifyAnalyzerAsync(source,
            new Microsoft.CodeAnalysis.Testing.DiagnosticResult(DiagnosticDescriptors.InvalidOptionsPathRule).WithSpan(2, 2, 2, 17 + path.Length).WithArguments(path));
    }

    [Fact]
    public async Task SameTypeAndName_ReportsDuplicate()
    {
        var source = """
            using Ling.AutoInject.Options;
            [AutoOptions("One", Name = "same")]
            [AutoOptions("Two", Name = "same")]
            public sealed class Options { }
            """;
        await Verify.VerifyAnalyzerAsync(source,
            new Microsoft.CodeAnalysis.Testing.DiagnosticResult(DiagnosticDescriptors.DuplicateOptionsRule).WithSpan(3, 2, 3, 35).WithArguments("Options", "same"));
    }
}
