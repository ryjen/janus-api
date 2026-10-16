#nullable enable

using System.Text;
using Amazon.Lambda.APIGatewayEvents;
using Janus;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Janus.Tests;

public sealed class StrictRecordRequestTests
{
    private static readonly RecordParsingLimits Limits = new(128, 8);
    private static readonly RecordFieldSchema Schema = new(
        new Dictionary<string, JTokenType> { ["rank"] = JTokenType.Integer });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ParsesPlainAndEncodedEnvelope(bool encoded)
    {
        const string json = "{\"rank\":2}";
        var request = new APIGatewayProxyRequest
        {
            Body = encoded ? System.Convert.ToBase64String(Encoding.UTF8.GetBytes(json)) : json,
            IsBase64Encoded = encoded
        };
        Assert.True(StrictRecordRequest.TryParse(request, Limits, Schema, out var mutation));
        Assert.NotNull(mutation);
        Assert.True(mutation.Fields.ContainsKey("rank"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RejectsNonStandardJsonInPlainAndBase64Envelopes(bool encoded)
    {
        const string json = "{\"rank\":2,}";
        var envelope = new APIGatewayProxyRequest
        {
            Body = encoded ? System.Convert.ToBase64String(Encoding.UTF8.GetBytes(json)) : json,
            IsBase64Encoded = encoded
        };

        Assert.False(StrictRecordRequest.TryParse(envelope, Limits, Schema, out var mutation));
        Assert.Null(mutation);
    }

    [Fact]
    public void RejectsInvalidEnvelopeWithoutMutation()
    {
        var request = new APIGatewayProxyRequest { Body = "!!!", IsBase64Encoded = true };
        Assert.False(StrictRecordRequest.TryParse(request, Limits, Schema, out var mutation));
        Assert.Null(mutation);
    }

    [Fact]
    public void RejectsEncodedUnknownFields()
    {
        var request = new APIGatewayProxyRequest
        {
            Body = System.Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"admin\":true}")),
            IsBase64Encoded = true
        };
        Assert.False(StrictRecordRequest.TryParse(request, Limits, Schema, out _));
    }

    [Fact]
    public void AppliesDecodedByteBudget()
    {
        var request = new APIGatewayProxyRequest
        {
            Body = System.Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"rank\":2}")),
            IsBase64Encoded = true
        };
        Assert.False(StrictRecordRequest.TryParse(request, new RecordParsingLimits(4, 8), Schema, out _));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RejectsDuplicateFieldsAcrossBothGatewayBodyRepresentations(bool encoded)
    {
        const string json = "{\"rank\":1,\"rank\":2}";
        var request = new APIGatewayProxyRequest
        {
            Body = encoded ? System.Convert.ToBase64String(Encoding.UTF8.GetBytes(json)) : json,
            IsBase64Encoded = encoded
        };
        Assert.False(StrictRecordRequest.TryParse(request, Limits, Schema, out var mutation));
        Assert.Null(mutation);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RejectsMalformedEscapedUnicodeAcrossBothGatewayBodyRepresentations(bool encoded)
    {
        var json = "{\"rank\":1,\"label\":\"" + "\\uD800" + "\"}";
        var request = new APIGatewayProxyRequest
        {
            Body = encoded ? System.Convert.ToBase64String(Encoding.UTF8.GetBytes(json)) : json,
            IsBase64Encoded = encoded
        };
        Assert.False(StrictRecordRequest.TryParse(request, Limits, Schema, out var mutation));
        Assert.Null(mutation);
    }

    [Fact]
    public void RejectsExcessiveNestingBeforeApplyingSchema()
    {
        var request = new APIGatewayProxyRequest
        {
            Body = "{\"rank\":{\"nested\":{\"tooDeep\":1}}}"
        };
        Assert.False(StrictRecordRequest.TryParse(
            request, new RecordParsingLimits(128, 2), Schema, out _));
    }
}
