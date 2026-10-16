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

    [Theory]
    [InlineData("{'rank':1}")]
    [InlineData("{rank:1}")]
    [InlineData("{\"rank\":1,}")]
    [InlineData("{\"data\":[1,2,]}")]
    [InlineData("{\"rank\":/* comment */1}")]
    [InlineData("{\"rank\":NaN}")]
    [InlineData("{\"rank\":Infinity}")]
    public void RejectsJsonNetJavaScriptExtensionsInOptInParser(string json)
    {
        Assert.False(RecordRequestParser.TryParseMutation(json, Limits, out var request));
        Assert.Null(request);
    }

    [Theory]
    [InlineData("{\"label\":\"/* text */\",\"rank\":1}")]
    [InlineData("{\"label\":\"it's JSON\",\"rank\":1}")]
    [InlineData("{\"label\":\"comma, and quote: \\\"\",\"rank\":1}")]
    public void PreservesValidJsonStringsContainingExtensionLikeCharacters(string json)
    {
        Assert.True(RecordRequestParser.TryParseMutation(json, Limits, out var request));
        Assert.NotNull(request);
    }

    [Fact]
    public void LegacyParserBehaviorNotChanged()
    {
        Assert.True(RecordRequestParser.TryParseMutation("{\"rank\":1,\"rank\":2}", out _));
    }

    [Theory]
    [InlineData("{\"id\":1,\"\\u0069d\":2}")]
    [InlineData("{\"outer\":{\"a\":1,\"\\u0061\":2}}")]
    public void RejectsEquivalentEscapedPropertyNames(string json)
    {
        Assert.False(RecordRequestParser.TryParseMutation(json, Limits, out _));
    }

    [Fact]
    public void UsesOrdinalPropertyNamesRatherThanCaseFolding()
    {
        Assert.True(RecordRequestParser.TryParseMutation("{\"level\":1,\"Level\":2}", Limits, out _));
    }

    [Theory]
    [InlineData("{\"rank\":1} {\"rank\":2}")]
    [InlineData("{\"rank\":1} trailing")]
    public void RejectsTrailingContent(string json)
    {
        Assert.False(RecordRequestParser.TryParseMutation(json, Limits, out _));
    }

    [Fact]
    public void RejectsUnpairedSurrogatesInOptInParser()
    {
        foreach (var codeUnit in new[] { 0xD800, 0xDC00 })
        {
            var json = "{\"name\":\"" + (char)codeUnit + "\"}";
            Assert.False(RecordRequestParser.TryParseMutation(json, Limits, out _));
        }
    }

    [Fact]
    public void RejectsUnpairedEscapedSurrogates()
    {
        foreach (var code in new[] { "D800", "DC00" })
        {
            var json = "{\"name\":\"" + "\\u" + code + "\"}";
            Assert.False(RecordRequestParser.TryParseMutation(json, Limits, out _));
        }
        Assert.False(RecordRequestParser.TryParseMutation(
            "{\"name\":\"" + "\\uD800" + "x\"}", Limits, out _));
    }

    [Fact]
    public void AcceptsValidSurrogatePairsAndEscapedBackslashes()
    {
        Assert.True(RecordRequestParser.TryParseMutation(
            "{\"name\":\"" + "\\uD83D\\uDE00" + "\"}", Limits, out _));
        Assert.True(RecordRequestParser.TryParseMutation(
            "{\"name\":\"" + "\\\\uD800" + "\"}", Limits, out _));
    }
}
