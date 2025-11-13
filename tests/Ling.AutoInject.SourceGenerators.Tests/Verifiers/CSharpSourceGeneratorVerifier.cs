using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;

namespace Ling.AutoInject.SourceGenerators.Tests.Verifiers;

internal static partial class CSharpSourceGeneratorVerifier<TSourceGenerator>
    where TSourceGenerator : new()
{
#if NET8_0_OR_GREATER
    [GeneratedRegex(@"\r?\n", RegexOptions.Compiled)]
    private static partial Regex NewLineRegex();
#else
    private static Regex NewLineRegex() => new(@"\r?\n", RegexOptions.Compiled);
#endif

    public static async Task VerifySourceGeneratorAsync(string source, params (string FileName, string GeneratedCode)[] generatedSources)
    {
        var test = new Test
        {
            TestState =
            {
                Sources = { source },
            },
        };

        foreach (var (fileName, generatedCode) in generatedSources)
        {
            var sourceText = NewLineRegex().Replace(generatedCode, Environment.NewLine);

            test.TestState.GeneratedSources.Add((typeof(TSourceGenerator), fileName, SourceText.From(sourceText, Encoding.UTF8)));
        }

        await test.RunAsync();
    }

    private class Test : CSharpSourceGeneratorTest<TSourceGenerator, DefaultVerifier>
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
