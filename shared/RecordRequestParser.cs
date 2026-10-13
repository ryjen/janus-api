#nullable enable

using Newtonsoft.Json;

namespace Janus;

// Compatibility-only parser for the historical v1 record API.
// Schema/size/depth restrictions belong in a separately versioned contract.
public sealed record RecordMutationRequest(IReadOnlyDictionary<string, object> Fields);

public static class RecordRequestParser
{
    public static bool TryParseMutation(string? body, out RecordMutationRequest? request)
    {
        request = null;
        if (!TryParseFields(body, out var fields)) return false;
        request = new RecordMutationRequest(fields!);
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
