#nullable enable

using Amazon.Lambda.APIGatewayEvents;
using Janus;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Janus.Tests;

// The strict parser is candidate v2-only. Do not change legacy v1 routing.
public sealed class StrictJsonGrammarTests
{
    private static readonly RecordParsingLimits Limits = new(1024, 16);
    private static readonly RecordFieldSchema ExampleSchema = new(
        new Dictionary<string, JTokenType>
        {
            ["rank"] = JTokenType.Integer,
            ["label"] = JTokenType.String
        });

    [Theory]
    [InlineData("/* comment */{\"rank\":1}")]
    [InlineData("{\"rank\":1/* comment */}")]
    [InlineData("{\"rank\":1// comment\n}")]
    [InlineData("{\"rank\":1,}")]
    [InlineData("{'rank':1}")]
    [InlineData("{rank:1}")]
    [InlineData("{\"rank\":01}")]
    [InlineData("{\"rank\":+1}")]
    [InlineData("{\"rank\":NaN}")]
    [InlineData("{\"rank\":.5}")]
    [InlineData("{\"rank\":1} {\"rank\":2}")]
    public void CandidateV2RejectsNonstandardJsonSyntax(string json)
    {
        Assert.False(RecordRequestParser.TryParseMutation(json, Limits, out var mutation));
        Assert.Null(mutation);
    }

    [Theory]
    [InlineData("{\"rank\":1,\"rank\":2}")]
    [InlineData("{\"outer\":{\"rank\":1,\"rank\":2}}")]
    [InlineData("{\"r\\u0061nk\":1,\"rank\":2}")]
    public void CandidateV2RejectsDuplicateDecodedPropertyNames(string json)
    {
        Assert.False(RecordRequestParser.TryParseMutation(json, Limits, out _));
    }

    [Fact]
    public void CandidateV2AcceptsEscapedQuotationAndCommentLikeTextInString()
    {
        const string json = "{\"label\":\"https://example.invalid//a/*literal*/\\\"quoted\\\"\"}";
        Assert.True(RecordRequestParser.TryParseMutation(json, Limits, ExampleSchema, out var request));
        Assert.NotNull(request);
        Assert.True(request.Fields.ContainsKey("label"));
    }

    [Fact]
    public void StrictGatewayPathAlsoRejectsCommentSyntax()
    {
        var envelope = new APIGatewayProxyRequest
        {
            Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
            Body = "{\"rank\":1,}"
        };
        Assert.False(StrictRecordRequest.TryParseJson(envelope, Limits, ExampleSchema, out var request));
        Assert.Null(request);
    }

    [Fact]
    public void LegacyCompatibilityPathStillAcceptsUnversionedFields()
    {
        Assert.True(RecordRequestParser.TryParseMutation("{\"legacy\":true}", out var mutation));
        Assert.NotNull(mutation);
    }
}
