using Microsoft.CodeAnalysis.CSharp;

namespace Ling.AutoInject.SourceGenerators.Helpers;

internal static class CSharpIdentifierHelper
{
    public static bool IsValidIdentifier(string? identifier)
    {
        return !string.IsNullOrEmpty(identifier) && SyntaxFacts.IsValidIdentifier(identifier);
    }

    public static bool IsValidIdentifierAllowAt(string? identifier)
    {
        if (string.IsNullOrEmpty(identifier)) return false;
        if (identifier![0] == '@')
        {
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
