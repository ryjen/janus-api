#nullable enable

using Amazon.Lambda.APIGatewayEvents;

namespace Janus;

// Candidate versioned HTTP boundary. Never substitute for the legacy handler
// without consumer migration, route versioning and deployed AWS qualification.
public static class StrictRecordRequest
{
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
