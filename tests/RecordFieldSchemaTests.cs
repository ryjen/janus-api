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

    // Json.NET normally guesses ISO timestamps as DateTime values. A JSON
    // string must remain a string under the future versioned schema policy.
    [Theory]
    [InlineData("2026-10-16T14:55:00Z")]
    [InlineData("2026-10-16T14:55:00+02:00")]
    [InlineData("2026-10-16")]
    public void VersionedSchemaPreservesIsoFormattedStringValues(string text)
    {
        var schema = new RecordFieldSchema(
            new Dictionary<string, JTokenType> { ["label"] = JTokenType.String });
        var json = "{\"label\":\"" + text + "\"}";

        Assert.True(schema.Matches(json));
        Assert.True(RecordRequestParser.TryParseMutation(
            json, Limits, schema, out var mutation));
        Assert.NotNull(mutation);
        Assert.Equal(text, Assert.IsType<string>(mutation.Fields["label"]));
    }

    [Fact]
    public void NestedIsoFormattedJsonStringRemainsAStringToken()
    {
        var schema = new RecordFieldSchema(
            new Dictionary<string, JTokenType> { ["state"] = JTokenType.Object });
        const string timestamp = "2026-10-16T14:55:00Z";
        const string json = "{\"state\":{\"recordedAt\":\"" + timestamp + "\"}}";

        Assert.True(RecordRequestParser.TryParseMutation(
            json, Limits, schema, out var mutation));
        Assert.NotNull(mutation);
        var nested = Assert.IsType<JObject>(mutation.Fields["state"]);
        Assert.Equal(JTokenType.String, nested["recordedAt"]?.Type);
        Assert.Equal(timestamp, nested.Value<string>("recordedAt"));
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
