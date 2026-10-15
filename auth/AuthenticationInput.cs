#nullable enable

namespace Janus;

// Authentication inputs must be JSON strings. Do not coerce objects/numbers
// or trim passwords, Cognito challenge sessions, or other opaque credentials.
public static class AuthenticationInput
{
    public static string? ReadRaw(IDictionary<string, object>? fields, string key)
    {
        if (fields is null || !fields.TryGetValue(key, out var value) ||
            value is not string text || string.IsNullOrWhiteSpace(text))
            return null;
        return text;
    }
}
