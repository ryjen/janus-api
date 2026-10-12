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
        public string? Json { get; set; }

        public Task<string?> ReadJsonAsync(string subject)
        {
            Calls++;
            if (Fail) throw new InvalidOperationException("Simulated outage");
            return Task.FromResult(Json);
        }

        public Task UpsertAsync(string subject, IReadOnlyDictionary<string, object> fields)
        {
            Calls++;
            if (Fail) throw new InvalidOperationException("Simulated outage");
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string subject)
        {
            Calls++;
            if (Fail) throw new InvalidOperationException("Simulated outage");
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
}
