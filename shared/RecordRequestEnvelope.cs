#nullable enable

using System.Text;

namespace Janus;

// Optional transport adapter for API Gateway's base64 body representation.
// Callers must select this explicitly; existing v1 handling is unchanged.
public static class RecordRequestEnvelope
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static bool TryDecode(
        string? body, bool isBase64Encoded, int maxDecodedBytes, out string? json)
    {
        json = null;
        if (maxDecodedBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxDecodedBytes));
        if (body is null) return false;

        if (!isBase64Encoded)
        {
            try
            {
                if (StrictUtf8.GetByteCount(body) > maxDecodedBytes) return false;
            }
            catch (EncoderFallbackException) { return false; }
            json = body;
            return true;
        }

        // Reject large envelopes before allocating decoded buffers.
        if (body.Length > 4L * ((maxDecodedBytes + 2L) / 3L)) return false;
        try
        {
            var bytes = Convert.FromBase64String(body);
            if (bytes.Length > maxDecodedBytes) return false;
            json = StrictUtf8.GetString(bytes);
            return true;
        }
        catch (FormatException) { return false; }
        catch (DecoderFallbackException) { return false; }
    }
}
