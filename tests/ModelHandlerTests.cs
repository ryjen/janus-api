#nullable enable

using Amazon.Lambda.APIGatewayEvents;
using Janus;
using Xunit;

namespace Janus.Tests;

// Handler contract tests exercise the real API Gateway envelope while replacing
// only the persistence adapter; no AWS credentials, network or Dynamo table.
public sealed class ModelHandlerTests
{
    private sealed class FakeStore : IRecordStore
    {
        public int Calls { get; private set; }
        public bool Fail { get; set; }
        public bool FailWithArgumentException { get; set; }
        public string? Json { get; set; }
        public IReadOnlyDictionary<string, object>? LastFields { get; private set; }

        public Task<string?> ReadJsonAsync(string subject)
        {
            Calls++;
            if (Fail) throw new InvalidOperationException("Simulated outage");
            if (FailWithArgumentException) throw new ArgumentException("Simulated storage argument error");
            return Task.FromResult(Json);
        }

        public Task UpsertAsync(string subject, IReadOnlyDictionary<string, object> fields)
        {
            Calls++;
            LastFields = fields;
            if (Fail) throw new InvalidOperationException("Simulated outage");
            if (FailWithArgumentException) throw new ArgumentException("Simulated storage argument error");
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string subject)
        {
            Calls++;
            if (Fail) throw new InvalidOperationException("Simulated outage");
            if (FailWithArgumentException) throw new ArgumentException("Simulated storage argument error");
            return Task.CompletedTask;
        }
    }

    private sealed class TestHandler : ModelHandler
    {
        public TestHandler(IRecordStore store, bool readOnly = false)
            : base("Player", readOnly, store) { }
    }

    private static APIGatewayProxyRequest Request(string method, string? subject = "owner", string? body = null) =>
        new()
        {
            HttpMethod = method,
            Body = body,
            RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
            {
                Authorizer = new APIGatewayCustomAuthorizerContext
                {
                    Claims = subject is null ? new Dictionary<string, string>() :
                        new Dictionary<string, string> { ["sub"] = subject }
                }
            }
        };

    [Fact]
    public async Task InfrastructureFailureReturns503NotCaller400()
    {
        var store = new FakeStore { Fail = true };
        var response = await new TestHandler(store).Handler(Request("GET"), null!);
        Assert.Equal(503, response.StatusCode);
        Assert.DoesNotContain("Simulated", response.Body);
        Assert.Equal(1, store.Calls);
    }

    [Theory]
    [InlineData("GET", null)]
    [InlineData("PUT", "{}")]
    public async Task StorageArgumentErrorsAre503Not400(string method, string? body)
    {
        var store = new FakeStore { FailWithArgumentException = true };
        var response = await new TestHandler(store).Handler(Request(method, body: body), null!);
        Assert.Equal(503, response.StatusCode);
        Assert.DoesNotContain("Simulated", response.Body);
        Assert.Equal(1, store.Calls);
    }

    [Fact]
    public async Task InvalidJsonReturns400WithoutPersistence()
    {
        var store = new FakeStore();
        var response = await new TestHandler(store).Handler(Request("PUT", body: "{bad"), null!);
        Assert.Equal(400, response.StatusCode);
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task AbsentSubjectReturns401WithoutPersistence()
    {
        var store = new FakeStore();
        var response = await new TestHandler(store).Handler(Request("GET", subject: null), null!);
        Assert.Equal(401, response.StatusCode);
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task ContradictoryIdReturns403WithoutPersistence()
    {
        var store = new FakeStore();
        var response = await new TestHandler(store).Handler(
            Request("PUT", body: "{\"id\":\"different\",\"rank\":4}"), null!);
        Assert.Equal(403, response.StatusCode);
        Assert.Equal(0, store.Calls);
    }

    [Theory]
    [InlineData("{\"id\":\"\"}")]
    [InlineData("{\"id\":\"  \"}")]
    public async Task ExplicitBlankBodyIdsAreForbidden(string body)
    {
        var store = new FakeStore();
        var response = await new TestHandler(store).Handler(Request("PUT", body: body), null!);
        Assert.Equal(403, response.StatusCode);
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task ReadReturnsExistingDatabaseJsonUnchanged()
    {
        const string json = "{\"id\":\"owner\",\"data\":{\"rank\":4}}";
        var store = new FakeStore { Json = json };
        var response = await new TestHandler(store).Handler(Request("GET"), null!);
        Assert.Equal(200, response.StatusCode);
        Assert.Equal(json, response.Body);
    }

    [Fact]
    public async Task ReadOnlyAccountRejectsWritesBeforeBodyParsing()
    {
        var store = new FakeStore();
        var response = await new TestHandler(store, readOnly: true).Handler(
            Request("POST", body: "{bad"), null!);
        Assert.Equal(405, response.StatusCode);
        Assert.Equal(0, store.Calls);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    public async Task LegacyPostAndPutBothUpsertAndReturnExistingResponse(string method)
    {
        var store = new FakeStore();
        var response = await new TestHandler(store).Handler(
            Request(method, body: "{\"rank\":4}"), null!);
        Assert.Equal(200, response.StatusCode);
        Assert.Contains("Player updated", response.Body);
        Assert.Equal(1, store.Calls);
        Assert.NotNull(store.LastFields);
        Assert.True(store.LastFields.ContainsKey("rank"));
    }

    [Theory]
    [InlineData("POST", null)]
    [InlineData("PUT", "")]
    [InlineData("DELETE", "  ")]
    public async Task EmptyMutationBodiesFailBeforeStore(string method, string? body)
    {
        var store = new FakeStore();
        var response = await new TestHandler(store).Handler(Request(method, body: body), null!);
        Assert.Equal(400, response.StatusCode);
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task UnknownMethodRejectsWithoutStore()
    {
        var store = new FakeStore();
        var response = await new TestHandler(store).Handler(Request("PATCH"), null!);
        Assert.Equal(405, response.StatusCode);
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public void ParserDoesNotAcceptNullOrMalformedJson()
    {
        Assert.False(RecordRequestParser.TryParseFields(null, out _));
        Assert.False(RecordRequestParser.TryParseFields("null", out _));
        Assert.False(RecordRequestParser.TryParseFields("{bad", out _));
    }
    [Fact]
    public void TypedMutationPreservesLegacyJsonFields()
    {
        Assert.True(RecordRequestParser.TryParseMutation("{\"rank\":4,\"label\":\"player\"}", out var request));
        Assert.NotNull(request);
        Assert.True(request.Fields.ContainsKey("rank"));
        Assert.True(request.Fields.ContainsKey("label"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("{bad")]
    public void TypedMutationRejectsInvalidBodies(string? body)
    {
        Assert.False(RecordRequestParser.TryParseMutation(body, out var request));
        Assert.Null(request);
    }


    [Fact]
    public void BoundedParserCountsUtf8BytesNotCharacters()
    {
        const string body = "{\"label\":\"é\"}";
        var limit = new RecordParsingLimits(System.Text.Encoding.UTF8.GetByteCount(body) - 1, 16);
        Assert.False(RecordRequestParser.TryParseMutation(body, limit, out _));
        Assert.True(RecordRequestParser.TryParseMutation(body,
            new RecordParsingLimits(System.Text.Encoding.UTF8.GetByteCount(body), 16), out var request));
        Assert.NotNull(request);
    }

    [Fact]
    public void BoundedParserRejectsDeeplyNestedInput()
    {
        const string body = "{\"a\":{\"b\":{\"c\":1}}}";
        Assert.False(RecordRequestParser.TryParseMutation(body,
            new RecordParsingLimits(1024, 2), out _));
        Assert.True(RecordRequestParser.TryParseMutation(body,
            new RecordParsingLimits(1024, 5), out _));
    }

    [Fact]
    public void BoundedParserDoesNotAffectLegacyEntryPoint()
    {
        const string body = "{\"message\":\"extended legacy payload\"}";
        Assert.False(RecordRequestParser.TryParseMutation(body,
            new RecordParsingLimits(10, 10), out _));
        Assert.True(RecordRequestParser.TryParseMutation(body, out _));
    }
}
