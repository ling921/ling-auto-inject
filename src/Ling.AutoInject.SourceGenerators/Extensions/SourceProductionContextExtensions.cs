using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace Ling.AutoInject.SourceGenerators.Extensions;

internal static class SourceProductionContextExtensions
{
    public static void AddSourceWithCrlf(
        this IncrementalGeneratorPostInitializationContext context,
        string hintName,
        string source)
    {
        context.AddSource(hintName, SourceText.From(NormalizeLineEndings(source), Encoding.UTF8));
    }

    public static void AddSourceWithCrlf(
        this SourceProductionContext context,
        string hintName,
        string source)
    {
        context.AddSource(hintName, SourceText.From(NormalizeLineEndings(source), Encoding.UTF8));
    }

    private static string NormalizeLineEndings(string source)
    {
        return source.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
    }
}
