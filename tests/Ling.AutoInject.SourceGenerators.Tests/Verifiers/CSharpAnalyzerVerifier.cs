using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using System.Collections.Immutable;

namespace Ling.AutoInject.SourceGenerators.Tests.Verifiers;

internal static class CSharpAnalyzerVerifier<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    public static DiagnosticResult Diagnostic()
        => CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic();

    public static DiagnosticResult Diagnostic(string diagnosticId)
        => CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic(diagnosticId);

    public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
        => CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic(descriptor);

    public static async Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    {
        var test = new Test { TestCode = source };
        test.ExpectedDiagnostics.AddRange(expected);

        test.TestState.Sources.Add(("AutoInjectConfigAttribute.g.cs", SourceCodes.AutoInjectConfigAttribute));
        test.TestState.Sources.Add(("AutoInjectExtensionsAttribute.g.cs", SourceCodes.AutoInjectExtensionsAttribute));
        test.TestState.Sources.Add(("SingletonServiceAttribute.g.cs", SourceCodes.SingletonServiceAttribute));
        test.TestState.Sources.Add(("ScopedServiceAttribute.g.cs", SourceCodes.ScopedServiceAttribute));
        test.TestState.Sources.Add(("TransientServiceAttribute.g.cs", SourceCodes.TransientServiceAttribute));

        await test.RunAsync();
    }

    private class Test : CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
    {
        protected override string DefaultTestProjectName => "TestProject";

        public Test()
        {
#if NET9_0_OR_GREATER
            TestState.ReferenceAssemblies = ReferenceAssemblies.Net.Net90
                .AddPackages([new PackageIdentity("Microsoft.Extensions.DependencyInjection.Abstractions", "9.0.11")]);
#elif NET8_0
            TestState.ReferenceAssemblies = ReferenceAssemblies.Net.Net80
                .AddPackages([new PackageIdentity("Microsoft.Extensions.DependencyInjection.Abstractions", "8.0.2")]);
#elif NET7_0
            TestState.ReferenceAssemblies = ReferenceAssemblies.Net.Net70
                .AddPackages([new PackageIdentity("Microsoft.Extensions.DependencyInjection.Abstractions", "7.0.0")]);
#elif NET6_0
            TestState.ReferenceAssemblies = ReferenceAssemblies.Net.Net60
                .AddPackages([new PackageIdentity("Microsoft.Extensions.DependencyInjection.Abstractions", "6.0.0")]);
#elif NET5_0
            TestState.ReferenceAssemblies = ReferenceAssemblies.Net.Net50
                .AddPackages([new PackageIdentity("Microsoft.Extensions.DependencyInjection.Abstractions", "5.0.0")]);
#elif NETCOREAPP3_1
            TestState.ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31
                .AddPackages([new PackageIdentity("Microsoft.Extensions.DependencyInjection.Abstractions", "3.1.32")]);
#endif
        }

        protected override ImmutableArray<(Project project, Diagnostic diagnostic)> FilterDiagnostics(ImmutableArray<(Project project, Diagnostic diagnostic)> diagnostics)
        {
            return diagnostics.Where(d => d.diagnostic.Id.StartsWith("LAI")).ToImmutableArray();
        }
    }
}
