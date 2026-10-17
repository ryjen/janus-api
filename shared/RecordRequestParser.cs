#nullable enable

using System.Text;
using System.Text.Json;

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
        if (!HasValidEscapedSurrogates(body)) return false;
        try
        {
            // Json.NET accepts non-standard JSON (comments, single-quoted
            // strings, unquoted property names and trailing commas). The
            // explicitly versioned boundary must validate RFC 8259 grammar
            // before invoking the legacy compatibility deserializer.
            // Byte allocation is bounded by the validated MaxUtf8Bytes above.
            var utf8 = StrictUtf8.GetBytes(body);
            var reader = new Utf8JsonReader(utf8, new JsonReaderOptions
            {
                MaxDepth = limits.MaxDepth,
                CommentHandling = JsonCommentHandling.Disallow,
                AllowTrailingCommas = false
            });
            var objectKeys = new Stack<HashSet<string>>();
            var rootSeen = false;
            var rootCompleted = false;
            var firstToken = true;
            while (reader.Read())
            {
                if (rootCompleted) return false; // no second top-level value
                if (firstToken)
                {
                    firstToken = false;
                    if (reader.TokenType != JsonTokenType.StartObject) return false;
                }
                switch (reader.TokenType)
                {
                    case JsonTokenType.StartObject:
                        if (objectKeys.Count == 0 && !rootSeen) rootSeen = true;
                        objectKeys.Push(new HashSet<string>(StringComparer.Ordinal));
                        break;
                    case JsonTokenType.PropertyName:
                        if (objectKeys.Count == 0 || !objectKeys.Peek().Add(reader.GetString()!))
                            return false;
                        break;
                    case JsonTokenType.EndObject:
                        if (objectKeys.Count == 0) return false;
                        objectKeys.Pop();
                        if (objectKeys.Count == 0) rootCompleted = true;
                        break;
                }
            }
            if (!rootSeen || !rootCompleted || objectKeys.Count != 0) return false;
        }
        catch (System.Text.Json.JsonException) { return false; }
        if (!TryParseMutation(body, out var parsed)) return false;
        request = parsed;
        return true;
    }

    // Schema application is opt-in; strict parsing runs first so duplicate
    // keys, malformed Unicode and size/depth violations cannot be normalized.
    public static bool TryParseMutation(string? body, RecordParsingLimits limits,
        RecordFieldSchema schema, out RecordMutationRequest? request)
    {
        request = null;
        ArgumentNullException.ThrowIfNull(schema);
        if (!TryParseMutation(body, limits, out var parsed)) return false;
        if (!schema.Matches(body!)) return false;
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
        catch (Newtonsoft.Json.JsonException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    // Inspect escape syntax before Json.NET can replace malformed surrogate escapes.
    // A literal escaped backslash (\\uD800) is not a Unicode escape.
    private static bool HasValidEscapedSurrogates(string json)
    {
        var inString = false;
        for (var i = 0; i < json.Length; i++)
        {
            var ch = json[i];
            if (!inString)
            {
                if (ch == '"') inString = true;
                continue;
            }
            if (ch == '"') { inString = false; continue; }
            if (ch != '\\') continue;
            if (++i >= json.Length) return false;
            if (json[i] != 'u') continue;
            if (!ReadHexCodeUnit(json, i + 1, out var code)) return false;
            i += 4;
            if (code is >= 0xDC00 and <= 0xDFFF) return false;
            if (code is < 0xD800 or > 0xDBFF) continue;
            if (i + 6 >= json.Length || json[i + 1] != '\\' || json[i + 2] != 'u' ||
                !ReadHexCodeUnit(json, i + 3, out var low) || low is < 0xDC00 or > 0xDFFF)
                return false;
            i += 6;
        }
        return true;
    }

    private static bool ReadHexCodeUnit(string json, int offset, out int value)
    {
        value = 0;
        if (offset + 4 > json.Length) return false;
        for (var j = 0; j < 4; j++)
        {
            var ch = json[offset + j];
            var digit = ch is >= '0' and <= '9' ? ch - '0'
                : ch is >= 'a' and <= 'f' ? ch - 'a' + 10
                : ch is >= 'A' and <= 'F' ? ch - 'A' + 10 : -1;
            if (digit < 0) return false;
            value = value * 16 + digit;
        }
        return true;
    }
}
