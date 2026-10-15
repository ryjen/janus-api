#nullable enable
namespace Janus;

// Unlike identifiers, passwords and Cognito challenge sessions are opaque.
// Validate the input's type without trimming or serializing it.
public static class AuthInput
{
    public static string? ReadOpaque(IDictionary<string, object>? values, string name)
    {
        if (values is null || !values.TryGetValue(name, out var value) ||
            value is not string text || string.IsNullOrWhiteSpace(text))
            return null;

        return text;
    }
}
