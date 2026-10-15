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
        if (envelope is null || !HasJsonContentType(envelope)) return false;
        return TryParse(envelope, limits, schema, out mutation);
    }

    private const int SingleValueCount = 1;
    private const int MediaTypeSegmentCount = 1;
    private const int MaxMediaTypeSegmentCount = 2;
    private const string ContentTypeHeader = "Content-Type";
    private const string JsonMediaType = "application/json";
    private const string Utf8CharsetParameter = "charset=utf-8";
    private const string QuotedUtf8CharsetParameter = "charset=\"utf-8\"";

    // API Gateway can populate both the single- and multi-value header views.
    // Accept at most one Content-Type in each view, and only when they agree.
    // An ambiguous header must never be used to select a strict v2 parser.
    private static bool HasJsonContentType(APIGatewayProxyRequest request)
    {
        string? single = null;
        var hasSingle = false;
        if (request.Headers is not null)
        {
            foreach (var header in request.Headers)
            {
                if (!string.Equals(header.Key, ContentTypeHeader, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (hasSingle || header.Value is null) return false;
                single = header.Value;
                hasSingle = true;
            }
        }

        string? multi = null;
        var hasMulti = false;
        if (request.MultiValueHeaders is not null)
        {
            foreach (var header in request.MultiValueHeaders)
            {
                if (!string.Equals(header.Key, ContentTypeHeader, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (hasMulti || header.Value is null || header.Value.Count != SingleValueCount ||
                    header.Value[0] is null)
                    return false;
                multi = header.Value[0];
                hasMulti = true;
            }
        }

        if (!hasSingle && !hasMulti) return false;
        if (hasSingle && hasMulti &&
            !string.Equals(single!.Trim(), multi!.Trim(), StringComparison.OrdinalIgnoreCase))
            return false;

        var parts = (single ?? multi)!.Split(';');
        if (parts.Length is < MediaTypeSegmentCount or > MaxMediaTypeSegmentCount ||
            !string.Equals(parts[0].Trim(), JsonMediaType, StringComparison.OrdinalIgnoreCase))
            return false;

        return parts.Length == MediaTypeSegmentCount ||
            string.Equals(parts[1].Trim(), Utf8CharsetParameter, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(parts[1].Trim(), QuotedUtf8CharsetParameter, StringComparison.OrdinalIgnoreCase);
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
