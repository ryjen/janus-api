#nullable enable

using Newtonsoft.Json;
using System.Text;

namespace Janus;

// Compatibility-only parser for the historical v1 record API.
// Schema/size/depth restrictions belong in a separately versioned contract.
public sealed record RecordMutationRequest(IReadOnlyDictionary<string, object> Fields);

// Opt-in limits for a future versioned contract. The deployed v1 path is unchanged.
public sealed record RecordParsingLimits(int MaxUtf8Bytes, int MaxDepth);

public static class RecordRequestParser
{
    public static bool TryParseMutation(string? body, out RecordMutationRequest? request)
    {
        request = null;
        if (!TryParseFields(body, out var fields)) return false;
        request = new RecordMutationRequest(fields!);
        return true;
    }

    // A separate policy-enabled entry point; do not enable for legacy v1 without a migration.
    public static bool TryParseMutation(string? body, RecordParsingLimits limits, out RecordMutationRequest? request)
    {
        request = null;
        if (limits is null || limits.MaxUtf8Bytes <= 0 || limits.MaxDepth <= 0)
            throw new ArgumentOutOfRangeException(nameof(limits));
        if (string.IsNullOrWhiteSpace(body)) return false;
        if (Encoding.UTF8.GetByteCount(body) > limits.MaxUtf8Bytes) return false;
        try
        {
            using var reader = new JsonTextReader(new StringReader(body)) { MaxDepth = limits.MaxDepth };
            while (reader.Read()) { }
        }
        catch (JsonException) { return false; }
        if (!TryParseMutation(body, out var parsed)) return false;
        request = parsed;
        return true;
    }

    // Retained for compatibility with existing callers and tests.
    public static bool TryParseFields(string? body, out Dictionary<string, object>? fields)
    {
        fields = null;
        if (string.IsNullOrWhiteSpace(body))
            return false;

        try
        {
            fields = body.ToParams();
            return fields is not null;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
