using Microsoft.CodeAnalysis.CSharp;

namespace Ling.AutoInject.SourceGenerators.Helpers;

internal static class CSharpIdentifierHelper
{
    public static string SanitizeIdentifier(string? value, string fallback = "Generated")
    {
        if (string.IsNullOrEmpty(value)) return fallback;

        var builder = new System.Text.StringBuilder(value!.Length);
        foreach (var character in value)
        {
            builder.Append(char.IsLetterOrDigit(character) || character == '_' ? character : '_');
        }

        var candidate = builder.Length == 0 ? fallback : builder.ToString();
        if (char.IsDigit(candidate[0])
            || !SyntaxFacts.IsValidIdentifier(candidate)
            || SyntaxFacts.GetKeywordKind(candidate) != SyntaxKind.None)
        {
            candidate = "_" + candidate;
        }

        return candidate;
    }

    public static bool IsValidIdentifier(string? identifier)
    {
        return !string.IsNullOrEmpty(identifier) && SyntaxFacts.IsValidIdentifier(identifier);
    }

    public static bool IsValidIdentifierAllowAt(string? identifier)
    {
        if (string.IsNullOrEmpty(identifier)) return false;
        if (identifier![0] == '@')
        {
            if (identifier.Length == 1) return false;
            var raw = identifier[1..];
            return SyntaxFacts.IsValidIdentifier(raw);
        }
        return SyntaxFacts.IsValidIdentifier(identifier);
    }

    public static bool IsValidNamespace(string? identifier)
    {
        if (string.IsNullOrEmpty(identifier)) return false;

        return identifier!.Split('.').All(IsValidIdentifier);
    }
}
