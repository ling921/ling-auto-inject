using Ling.AutoInject.SourceGenerators.Analyzers;
using Ling.AutoInject.SourceGenerators.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Ling.AutoInject.SourceGenerators.Tests.Verifiers.CSharpAnalyzerVerifier<
    Ling.AutoInject.SourceGenerators.Analyzers.AutoInjectAttributeAnalyzer>;

namespace Ling.AutoInject.SourceGenerators.Tests.Analyzers;

/// <summary>
/// Tests for <see cref="AutoInjectAttributeAnalyzer"/>.
/// </summary>
public sealed class AutoInjectAttributeAnalyzerTests
{
    #region Duplicate Attributes

    [Theory]
    [InlineData("Singleton")]
    [InlineData("Scoped")]
    [InlineData("Transient")]
    public async Task DuplicatePositionalAttributes_ReportsDiagnostic(string lifetime)
    {
        var source = $$"""
            using Ling.AutoInject;

            namespace Test
            {
                [{{lifetime}}Service]
                [{{lifetime}}Service]
                public class MyService
                {
                }
            }
            """;

        var dr = new DiagnosticResult(DiagnosticDescriptors.DuplicateAttributeRule)
            .WithLocation(6, 6)
            .WithArguments(lifetime);
        await VerifyCS.VerifyAnalyzerAsync(source, dr);
    }

    [Theory]
    [InlineData("Singleton")]
    [InlineData("Scoped")]
    [InlineData("Transient")]
    public async Task DuplicatePositionalAndEmptyCtor_ReportsDiagnostic(string lifetime)
    {
        var source = $$"""
            using Ling.AutoInject;

            namespace Test
            {
                [{{lifetime}}Service]
                [{{lifetime}}Service()]
                public class MyService
                {
                }
            }
            """;

        var dr = new DiagnosticResult(DiagnosticDescriptors.DuplicateAttributeRule)
            .WithLocation(6, 6)
            .WithArguments(lifetime);
        await VerifyCS.VerifyAnalyzerAsync(source, dr);
    }

    [Theory]
    [InlineData("Singleton")]
    [InlineData("Scoped")]
    [InlineData("Transient")]
    public async Task DuplicateWithServiceType_ReportsDiagnostic(string lifetime)
    {
        var source = $$"""
            using Ling.AutoInject;

            namespace Test
            {
                public interface IFoo { }

                [{{lifetime}}Service(typeof(IFoo))]
                [{{lifetime}}Service(typeof(IFoo))]
                public class MyService : IFoo
                {
                }
            }
            """;

        var dr = new DiagnosticResult(DiagnosticDescriptors.DuplicateAttributeRule)
            .WithLocation(8, 6)
            .WithArguments(lifetime);
        await VerifyCS.VerifyAnalyzerAsync(source, dr);
    }

    [Theory]
    [InlineData("Singleton")]
    [InlineData("Scoped")]
    [InlineData("Transient")]
    public async Task DuplicateWithServiceKey_ReportsDiagnostic(string lifetime)
    {
        var source = $$"""
            using Ling.AutoInject;

            namespace Test
            {
                [{{lifetime}}Service(ServiceKey = "k1")]
                [{{lifetime}}Service(ServiceKey = "k1")]
                public class MyService
                {
                }
            }
            """;

        var dr = new DiagnosticResult(DiagnosticDescriptors.DuplicateAttributeRule)
            .WithLocation(6, 6)
            .WithArguments(lifetime);
        await VerifyCS.VerifyAnalyzerAsync(source, dr);
    }

    #endregion Duplicate Attributes

    #region Conflicting Lifetimes

    [Fact]
    public async Task ConflictingLifetimes_TwoDifferent_ReportsDiagnostic()
    {
        var source = $$"""
            using Ling.AutoInject;

            namespace Test
            {
                [SingletonService]
                [ScopedService]
                public class MyService
                {
                }
            }
            """;
        var dr = new DiagnosticResult(DiagnosticDescriptors.ConflictingLifetimeRule)
            .WithLocation(6, 6)
            .WithArguments("Singleton", "Scoped");
        await VerifyCS.VerifyAnalyzerAsync(source, dr);
    }

    [Fact]
    public async Task ConflictingLifetimes_ThreeDifferent_ReportsDiagnostic()
    {
        var source = $$"""
            using Ling.AutoInject;

            namespace Test
            {
                [SingletonService]
                [ScopedService]
                [TransientService]
                public class MyService
                {
                }
            }
            """;
        var dr1 = new DiagnosticResult(DiagnosticDescriptors.ConflictingLifetimeRule)
            .WithLocation(6, 6)
            .WithArguments("Singleton", "Scoped");
        var dr2 = new DiagnosticResult(DiagnosticDescriptors.ConflictingLifetimeRule)
            .WithLocation(7, 6)
            .WithArguments("Singleton", "Transient");
        await VerifyCS.VerifyAnalyzerAsync(source, dr1, dr2);
    }

    #endregion Conflicting Lifetimes

    #region ServiceType Mismatch

    [Theory]
    [InlineData("Singleton")]
    [InlineData("Scoped")]
    [InlineData("Transient")]
    public async Task ServiceTypeMismatch_WhenNotImplemented_ReportsDiagnostic(string lifetime)
    {
        var source = $$"""
            using Ling.AutoInject;

            namespace Test
            {
                public interface IFoo { }

                [{{lifetime}}Service(typeof(IFoo))]
                public class MyService
                {
                }
            }
            """;

        var dr = new DiagnosticResult(DiagnosticDescriptors.ServiceTypeMismatchRule)
            .WithLocation(7, 6)
            .WithArguments("IFoo", "MyService");
        await VerifyCS.VerifyAnalyzerAsync(source, dr);
    }

    #endregion ServiceType Mismatch
}
