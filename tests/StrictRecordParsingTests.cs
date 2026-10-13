#nullable enable

using Janus;
using Xunit;

namespace Janus.Tests;

public sealed class StrictRecordParsingTests
{
    private static readonly RecordParsingLimits Limits = new(4096, 16);

    [Theory]
    [InlineData("{\"rank\":1,\"rank\":2}")]
    [InlineData("{\"data\":{\"level\":1,\"level\":2}}")]
    [InlineData("{\"data\":[{\"id\":1,\"id\":2}]}")]
    public void RejectsDuplicateKeysAtAnyNestingLevel(string json)
    {
        Assert.False(RecordRequestParser.TryParseMutation(json, Limits, out _));
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("[{\"rank\":1}]")]
    [InlineData("\"string\"")]
    [InlineData("42")]
    [InlineData("null")]
    public void RejectsNonObjectRoot(string json)
    {
        Assert.False(RecordRequestParser.TryParseMutation(json, Limits, out _));
    }

    [Fact]
    public void AllowsSamePropertyNameInSeparateObjects()
    {
        const string json = "{\"first\":{\"level\":1},\"second\":{\"level\":2}}";
        Assert.True(RecordRequestParser.TryParseMutation(json, Limits, out var parsed));
        Assert.NotNull(parsed);
    }

    [Fact]
    public void LegacyParserBehaviorNotChanged()
    {
        Assert.True(RecordRequestParser.TryParseMutation("{\"rank\":1,\"rank\":2}", out _));
    }
}
