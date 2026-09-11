using Ling.AutoInject.SourceGenerators.Diagnostics;
using Verify = Ling.AutoInject.SourceGenerators.Tests.Verifiers.CSharpAnalyzerVerifier<
    Ling.AutoInject.SourceGenerators.Analyzers.AutoInjectModuleAnalyzer>;

namespace Ling.AutoInject.SourceGenerators.Tests.Analyzers;

public sealed class AutoInjectModuleAnalyzerTests
{
    [Fact]
    public async Task NonStaticOrPartialModule_ReportsDiagnostic()
    {
        const string source = """
            using Ling.AutoInject;

            [AutoInjectModule]
            public class Services { }
            """;

        await Verify.VerifyAnalyzerAsync(source,
            new Microsoft.CodeAnalysis.Testing.DiagnosticResult(DiagnosticDescriptors.RequiredStaticPartialClassRule)
                .WithLocation(3, 2).WithArguments("Services"));
    }

    [Fact]
    public async Task RegistrationReferencingNonModule_ReportsDiagnostic()
    {
        const string source = """
            using Ling.AutoInject;

            public static partial class NotAModule { }

            [SingletonService(Module = typeof(NotAModule))]
            public sealed class Service { }
            """;

        await Verify.VerifyAnalyzerAsync(source,
            new Microsoft.CodeAnalysis.Testing.DiagnosticResult(DiagnosticDescriptors.InvalidModuleReferenceRule)
                .WithLocation(5, 2).WithArguments("Module 'NotAModule' must be a non-generic top-level static partial class marked with [AutoInjectModule]."));
    }
}
