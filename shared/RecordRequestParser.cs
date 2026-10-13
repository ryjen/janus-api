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
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

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
        // Fail closed on malformed UTF-16 instead of replacement-fallback encoding.
        try
        {
            if (StrictUtf8.GetByteCount(body) > limits.MaxUtf8Bytes) return false;
        }
        catch (EncoderFallbackException) { return false; }
        try
        {
            using var reader = new JsonTextReader(new StringReader(body)) { MaxDepth = limits.MaxDepth };
            var objectKeys = new Stack<HashSet<string>>();
            var rootSeen = false;
            var rootCompleted = false;
            var firstToken = true;
            while (reader.Read())
            {
                if (firstToken)
                {
                    firstToken = false;
                    if (reader.TokenType != JsonToken.StartObject) return false;
                }
                switch (reader.TokenType)
                {
                    case JsonToken.StartObject:
                        if (objectKeys.Count == 0 && !rootSeen) rootSeen = true;
                        objectKeys.Push(new HashSet<string>(StringComparer.Ordinal));
                        break;
                    case JsonToken.PropertyName:
                        if (objectKeys.Count == 0 || !objectKeys.Peek().Add((string)reader.Value!))
                            return false;
                        break;
                    case JsonToken.EndObject:
                        if (objectKeys.Count == 0) return false;
                        objectKeys.Pop();
                        if (objectKeys.Count == 0) rootCompleted = true;
                        break;
                }
            }
            if (!rootSeen || !rootCompleted || objectKeys.Count != 0) return false;
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
