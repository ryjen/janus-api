#nullable enable

using Amazon.Lambda.APIGatewayEvents;

namespace Janus;

// Candidate versioned HTTP boundary. Never substitute for the legacy handler
// without consumer migration, route versioning and deployed AWS qualification.
public static class StrictRecordRequest
{
    // For an explicitly versioned HTTP contract: require a JSON media type,
    // including when API Gateway base64-encodes the body. v1 does not call this.
    public static bool TryParseJson(
        APIGatewayProxyRequest? envelope,
        RecordParsingLimits limits,
        RecordFieldSchema schema,
        out RecordMutationRequest? mutation)
    {
        mutation = null;
        if (envelope is null || !HasJsonContentType(envelope.Headers)) return false;
        return TryParse(envelope, limits, schema, out mutation);
    }

    private static bool HasJsonContentType(IDictionary<string, string>? headers)
    {
        if (headers is null) return false;
        string? contentType = null;
        foreach (var header in headers)
        {
            if (!string.Equals(header.Key, "Content-Type", StringComparison.OrdinalIgnoreCase))
                continue;
            if (contentType is not null) return false; // ambiguous casing duplicates
            contentType = header.Value;
        }
        if (contentType is null) return false;
        var parts = contentType.Split(';');
        if (!string.Equals(parts[0].Trim(), "application/json", StringComparison.OrdinalIgnoreCase))
            return false;
        for (var i = 1; i < parts.Length; i++)
        {
            var parameter = parts[i].Trim();
            if (!string.Equals(parameter, "charset=utf-8", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(parameter, "charset=\"utf-8\"", StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }

    public static bool TryParse(
        APIGatewayProxyRequest? envelope,
        RecordParsingLimits limits,
        RecordFieldSchema schema,
        out RecordMutationRequest? mutation)
    {
        mutation = null;
        ArgumentNullException.ThrowIfNull(limits);
        ArgumentNullException.ThrowIfNull(schema);
        if (envelope is null) return false;

        if (!RecordRequestEnvelope.TryDecode(
                envelope.Body, envelope.IsBase64Encoded, limits.MaxUtf8Bytes, out var json))
            return false;

        return RecordRequestParser.TryParseMutation(json, limits, schema, out mutation);
    }
}
