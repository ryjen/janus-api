#nullable enable

using Janus;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Janus.Tests;

public sealed class RecordFieldSchemaTests
{
    private static readonly RecordParsingLimits Limits = new(1024, 16);
    // Example contract only; not an approved player DTO or deployed schema.
    private static readonly RecordFieldSchema Example = new(
        new Dictionary<string, JTokenType>
        {
            ["rank"] = JTokenType.Integer,
            ["label"] = JTokenType.String
        });

    [Fact]
    public void AllowsExplicitlyTypedFields()
    {
        Assert.True(RecordRequestParser.TryParseMutation(
            "{\"rank\":4,\"label\":\"guest\"}", Limits, Example, out var request));
        Assert.NotNull(request);
    }

    [Theory]
    [InlineData("{\"rank\":\"4\"}")]
    [InlineData("{\"rank\":null}")]
    [InlineData("{\"extra\":true}")]
    [InlineData("{\"Rank\":4}")]
    public void RejectsUnknownFieldsAndWrongTypes(string json)
    {
        Assert.False(RecordRequestParser.TryParseMutation(json, Limits, Example, out _));
    }

    [Fact]
    public void SchemaCannotOverrideStrictParserSecurityChecks()
    {
        Assert.False(RecordRequestParser.TryParseMutation(
            "{\"rank\":1,\"rank\":2}", Limits, Example, out _));
    }

    [Fact]
    public void LegacyV1PathRemainsPermissive()
    {
        Assert.True(RecordRequestParser.TryParseMutation("{\"extra\":true}", out _));
    }
}
