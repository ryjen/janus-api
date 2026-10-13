#nullable enable

using Amazon.Lambda.APIGatewayEvents;
using Janus;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Janus.Tests;

public sealed class StrictContentTypeTests
{
    private static readonly RecordParsingLimits Limits = new(512, 8);
    private static readonly RecordFieldSchema Schema =
        new(new Dictionary<string, JTokenType> { ["rank"] = JTokenType.Integer });

    private static APIGatewayProxyRequest Make(string? contentType)
    {
        var headers = new Dictionary<string, string>();
        if (contentType is not null) headers["content-type"] = contentType;
        return new APIGatewayProxyRequest { Body = "{\"rank\":2}", Headers = headers };
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("Application/JSON; charset=utf-8")]
    [InlineData("application/json; charset=\"utf-8\"")]
    public void AcceptsSupportedJsonContentType(string contentType)
    {
        Assert.True(StrictRecordRequest.TryParseJson(Make(contentType), Limits, Schema, out _));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("text/plain")]
    [InlineData("application/problem+json")]
    [InlineData("application/json; charset=iso-8859-1")]
    [InlineData("application/json, text/plain")]
    public void RejectsMissingOrUnsupportedContentType(string? contentType)
    {
        Assert.False(StrictRecordRequest.TryParseJson(Make(contentType), Limits, Schema, out _));
    }

    [Fact]
    public void RejectsAmbiguousCasingDuplicates()
    {
        var request = Make("application/json");
        request.Headers["Content-Type"] = "text/plain";
        Assert.False(StrictRecordRequest.TryParseJson(request, Limits, Schema, out _));
    }

    [Fact]
    public void LegacyOptionalPipelineDoesNotRequireContentType()
    {
        Assert.True(StrictRecordRequest.TryParse(Make(null), Limits, Schema, out _));
    }
}
