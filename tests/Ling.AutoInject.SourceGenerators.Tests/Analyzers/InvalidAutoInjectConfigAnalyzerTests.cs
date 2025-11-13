using Ling.AutoInject.SourceGenerators.Analyzers;
using Ling.AutoInject.SourceGenerators.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Ling.AutoInject.SourceGenerators.Tests.Verifiers.CSharpAnalyzerVerifier<
    Ling.AutoInject.SourceGenerators.Analyzers.InvalidAutoInjectConfigAnalyzer>;

namespace Ling.AutoInject.SourceGenerators.Tests.Analyzers;

/// <summary>
/// Tests for <see cref="InvalidAutoInjectConfigAnalyzer"/>.
/// </summary>
public class InvalidAutoInjectConfigAnalyzerTests
{
    [Theory]
    [InlineData("MethodName", "MethodName")]
    [InlineData("MethodName", "methodName")]
    [InlineData("MethodName", "Method_Name")]
    [InlineData("MethodName", "@MethodName")]
    [InlineData("MethodName", "_MethodName")]
    [InlineData("ClassName", "ClassName")]
    [InlineData("ClassName", "className")]
    [InlineData("ClassName", "Class_Name")]
    [InlineData("ClassName", "@ClassName")]
    [InlineData("ClassName", "_ClassName")]
    [InlineData("Namespace", "MyNamespace")]
    [InlineData("Namespace", "myNamespace")]
    [InlineData("Namespace", "My_Namespace")]
    [InlineData("Namespace", "_MyNamespace")]
    [InlineData("Namespace", "My.Namespace")]
    [InlineData("Namespace", "My.Namespace.ABC")]
    public async Task AutoInjectConfig_ValidArgument_ReportsNoDiagnostic(string parameter, string value)
    {
        var source = $"[assembly: Ling.AutoInject.AutoInjectConfig({parameter} = \"{value}\")]";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Theory]
    [InlineData("MethodName", "Method-Name")]
    [InlineData("MethodName", "Method.Name")]
    [InlineData("MethodName", "Method@Name")]
    [InlineData("MethodName", "123MethodName")]
    [InlineData("ClassName", "Class-Name")]
    [InlineData("ClassName", "Class.Name")]
    [InlineData("ClassName", "Class@Name")]
    [InlineData("ClassName", "123ClassName")]
    [InlineData("Namespace", "My Namespace")]
    [InlineData("Namespace", "@MyNamespace")]
    [InlineData("Namespace", "My.Bad-Namespace")]
    public async Task AutoInjectConfig_InvalidArgument_ReportsDiagnostic(string parameter, string value)
    {
        var source = $"[assembly: Ling.AutoInject.AutoInjectConfig({parameter} = \"{value}\")]";

        var dr = new DiagnosticResult(DiagnosticDescriptors.InvalidConfigRule)
            .WithLocation(1, parameter.Length + 48)
            .WithArguments(parameter, value);
        await VerifyCS.VerifyAnalyzerAsync(source, dr);
    }
}
