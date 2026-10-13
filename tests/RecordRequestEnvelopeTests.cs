#nullable enable

using System.Text;
using Janus;
using Xunit;

namespace Janus.Tests;

public sealed class RecordRequestEnvelopeTests
{
    [Fact]
    public void AcceptsPlainJsonWithinUtf8ByteLimit()
    {
        const string json = "{\"label\":\"é\"}";
        var bytes = Encoding.UTF8.GetByteCount(json);
        Assert.False(RecordRequestEnvelope.TryDecode(json, false, bytes - 1, out _));
        Assert.True(RecordRequestEnvelope.TryDecode(json, false, bytes, out var decoded));
        Assert.Equal(json, decoded);
    }

    [Fact]
    public void DecodesBase64WithStrictUtf8()
    {
        const string json = "{\"rank\":4}";
        var encoded = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        Assert.True(RecordRequestEnvelope.TryDecode(encoded, true, 64, out var decoded));
        Assert.Equal(json, decoded);
    }

    [Fact]
    public void RejectsOversizedMalformedAndNonUtf8Base64()
    {
        Assert.False(RecordRequestEnvelope.TryDecode("!!!", true, 8, out _));
        Assert.False(RecordRequestEnvelope.TryDecode(System.Convert.ToBase64String(new byte[] { 0xff }), true, 8, out _));
        Assert.False(RecordRequestEnvelope.TryDecode(System.Convert.ToBase64String(new byte[64]), true, 16, out _));
    }

    [Fact]
    public void RejectsInvalidUnpairedSurrogate()
    {
        Assert.False(RecordRequestEnvelope.TryDecode("\uD800", false, 64, out _));
    }
}
