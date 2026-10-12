#nullable enable

namespace Janus;

// Parse caller input without allowing nulls, object values, or whitespace-only fields.
public static class PasswordRecoveryInput
{
    public static string? Read(IDictionary<string, object>? request, string key)
    {
        if (request is null || !request.TryGetValue(key, out var value) || value is not string text)
            return null;

        var trimmed = text.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }
}
