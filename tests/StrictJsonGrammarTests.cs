#nullable enable

using Amazon.Lambda.APIGatewayEvents;
using Janus;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Janus.Tests;

// The strict parser is candidate v2-only. Do not change legacy v1 routing.
public sealed class StrictJsonGrammarTests
{
    private const int TestMaxUtf8Bytes = 1024;
    private const int TestMaxDepth = 16;
    private static readonly RecordParsingLimits Limits = new(TestMaxUtf8Bytes, TestMaxDepth);
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
    [InlineData("{\"rank\":Infinity}")]
    [InlineData("{\"rank\":1,\"data\":[1,2,]}")]
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

    [Theory]
    [InlineData("{\"rank\":1,\"\\u0072ank\":2}")]
    [InlineData("{\"meta\":[{\"rank\":1,\"r\\u0061nk\":2}]}")]
    [InlineData("{\"\\u0061\":1,\"a\":2}")]
    public void CandidateV2RejectsEquivalentEscapedKeysInNestedScopes(string json)
    {
        Assert.False(RecordRequestParser.TryParseMutation(json, Limits, out _));
    }

    [Theory]
    [InlineData("{\"label\":\"\\uD800\"}")]
    [InlineData("{\"label\":\"\\uDC00\"}")]
    [InlineData("{\"label\":\"\\uD800\\uD800\"}")]
    [InlineData("{\"label\":\"\\uD800\\u0041\"}")]
    [InlineData("{\"label\":\"\\u0041\\uDC00\"}")]
    public void CandidateV2RejectsUnpairedEscapedSurrogateCodeUnits(string json)
    {
        Assert.False(RecordRequestParser.TryParseMutation(json, Limits, out _));
    }

    [Fact]
    public void CandidateV2AcceptsPairedEscapedSurrogates()
    {
        const string json = "{\"label\":\"\\uD83D\\uDE00\"}";
        Assert.True(RecordRequestParser.TryParseMutation(json, Limits, ExampleSchema, out _));
    }

    [Fact]
    public void CandidateV2DoesNotTreatEscapedBackslashAsUnicodeSequence()
    {
        const string json = "{\"label\":\"\\\\uD800\"}";
        Assert.True(RecordRequestParser.TryParseMutation(json, Limits, ExampleSchema, out _));
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
            Body = "{\"rank\":1/* comment */}"
        };
        Assert.False(StrictRecordRequest.TryParseJson(envelope, Limits, ExampleSchema, out var request));
        Assert.Null(request);
    }

    [Fact]
    public void CandidateV2AcceptsValidNestedJsonWithTrailingWhitespace()
    {
        const string json = "{\"rank\":1,\"data\":[{\"level\":2}]} \r\n\t";
        Assert.True(RecordRequestParser.TryParseMutation(json, Limits, out var request));
        Assert.NotNull(request);
        Assert.True(request.Fields.ContainsKey("rank"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StrictGatewayAcceptsValidJsonInBothBodyRepresentations(bool encoded)
    {
        const string json = "{\"rank\":2,\"label\":\"example\"} \n";
        var envelope = new APIGatewayProxyRequest
        {
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            },
            Body = encoded
                ? System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json))
                : json,
            IsBase64Encoded = encoded
        };
        Assert.True(StrictRecordRequest.TryParseJson(envelope, Limits, ExampleSchema, out var mutation));
        Assert.NotNull(mutation);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StrictGatewayRejectsTrailingCommaInBothBodyRepresentations(bool encoded)
    {
        const string json = "{\"rank\":2,}";
        var envelope = new APIGatewayProxyRequest
        {
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            },
            Body = encoded
                ? System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json))
                : json,
            IsBase64Encoded = encoded
        };
        Assert.False(StrictRecordRequest.TryParseJson(envelope, Limits, ExampleSchema, out var mutation));
        Assert.Null(mutation);
    }

    [Theory]
    [InlineData("{\"label\":\"raw\nnewline\"}")]
    [InlineData("{\"rank\":1.}")]
    [InlineData("{\"rank\":1e}")]
    [InlineData("{\"rank\":-}")]
    [InlineData("{\"rank\":1}not-json")]
    public void CandidateV2RejectsAdditionalMalformedGrammar(string json)
    {
        Assert.False(RecordRequestParser.TryParseMutation(json, Limits, out var request));
        Assert.Null(request);
    }

    [Fact]
    public void CandidateV2CountsNestedArraysTowardsDepthLimit()
    {
        const string json = "{\"data\":[[[1]]]}";
        Assert.False(RecordRequestParser.TryParseMutation(
            json, new RecordParsingLimits(TestMaxUtf8Bytes, 3), out _));
        Assert.True(RecordRequestParser.TryParseMutation(
            json, new RecordParsingLimits(TestMaxUtf8Bytes, 5), out _));
    }

    [Fact]
    public void CandidateV2CountsActualUtf8BytesForSupplementaryUnicode()
    {
        const string json = "{\"label\":\"😀\"}";
        var exactBytes = System.Text.Encoding.UTF8.GetByteCount(json);
        Assert.False(RecordRequestParser.TryParseMutation(
            json, new RecordParsingLimits(exactBytes - 1, TestMaxDepth), out _));
        Assert.True(RecordRequestParser.TryParseMutation(
            json, new RecordParsingLimits(exactBytes, TestMaxDepth), out _));
    }

    [Fact]
    public void CandidateV2AllowsRepeatedNamesInSeparateObjects()
    {
        const string json = "{\"data\":[{\"score\":1},{\"score\":2}]}";
        Assert.True(RecordRequestParser.TryParseMutation(json, Limits, out _));
    }

    [Fact]
    public void CandidateV2UsesOrdinalPropertyNameIdentity()
    {
        const string json = "{\"rank\":1,\"Rank\":2}";
        Assert.True(RecordRequestParser.TryParseMutation(json, Limits, out _));
    }

    [Fact]
    public void LegacyCompatibilityPathStillAcceptsUnversionedFields()
    {
        Assert.True(RecordRequestParser.TryParseMutation("{\"legacy\":true}", out var mutation));
        Assert.NotNull(mutation);
    }
}
