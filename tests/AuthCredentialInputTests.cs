#nullable enable

using Amazon;
using Amazon.Runtime;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Lambda.APIGatewayEvents;
using Janus;
using Newtonsoft.Json;
using Xunit;

namespace Janus.Tests;

public sealed class AuthCredentialInputTests
{
    private sealed class RecordingCognito : AmazonCognitoIdentityProviderClient
    {
        public AdminInitiateAuthRequest? Initiated { get; private set; }
        public AdminRespondToAuthChallengeRequest? Challenged { get; private set; }
        public int Calls { get; private set; }

        public RecordingCognito() : base(new AnonymousAWSCredentials(), RegionEndpoint.USEast1) { }

        public override Task<AdminInitiateAuthResponse> AdminInitiateAuthAsync(
            AdminInitiateAuthRequest request, CancellationToken cancellationToken = default)
        {
            Calls++;
            Initiated = request;
            return Task.FromResult(new AdminInitiateAuthResponse
            {
                AuthenticationResult = new AuthenticationResultType { IdToken = "test-id-token" }
            });
        }

        public override Task<AdminRespondToAuthChallengeResponse> AdminRespondToAuthChallengeAsync(
            AdminRespondToAuthChallengeRequest request, CancellationToken cancellationToken = default)
        {
            Calls++;
            Challenged = request;
            return Task.FromResult(new AdminRespondToAuthChallengeResponse
            {
                AuthenticationResult = new AuthenticationResultType { IdToken = "test-id-token" }
            });
        }
    }

    private static APIGatewayProxyRequest Request(string path, IDictionary<string, object> values) => new()
    {
        Path = path,
        Body = JsonConvert.SerializeObject(values)
    };

    private static Dictionary<string, object> Values() => new()
    {
        ["email"] = " player@example.invalid ",
        ["password"] = " leading and trailing ",
        ["session"] = " opaque session "
    };

    [Theory]
    [InlineData("/authenticate", "email")]
    [InlineData("/authenticate", "password")]
    [InlineData("/reset", "email")]
    [InlineData("/reset", "password")]
    [InlineData("/reset", "session")]
    public async Task RejectsNonStringCredentialsBeforeCognito(string path, string key)
    {
        using var fake = new RecordingCognito();
        var values = Values();
        values[key] = new { nested = 123 };
        var response = await new Auth(fake, "test-pool", "test-client")
            .Handler(Request(path, values), null!);

        Assert.Equal(401, response.StatusCode);
        Assert.Equal(0, fake.Calls);
        Assert.DoesNotContain("nested", response.Body);
    }

    [Theory]
    [InlineData("/authenticate")]
    [InlineData("/reset")]
    public async Task RejectsMissingPasswordBeforeCognito(string path)
    {
        using var fake = new RecordingCognito();
        var values = Values();
        values.Remove("password");
        var response = await new Auth(fake, "test-pool", "test-client")
            .Handler(Request(path, values), null!);

        Assert.Equal(401, response.StatusCode);
        Assert.Equal(0, fake.Calls);
    }

    [Fact]
    public async Task SuccessfulAuthenticationPreservesOpaquePassword()
    {
        using var fake = new RecordingCognito();
        var response = await new Auth(fake, "test-pool", "test-client")
            .Handler(Request("/authenticate", Values()), null!);

        Assert.Equal(200, response.StatusCode);
        Assert.Equal(" leading and trailing ", fake.Initiated!.AuthParameters["PASSWORD"]);
        Assert.Equal(" player@example.invalid ", fake.Initiated.AuthParameters["USERNAME"]);
        Assert.Equal(1, fake.Calls);
    }

    [Fact]
    public async Task SuccessfulPasswordChallengePreservesSessionAndPassword()
    {
        using var fake = new RecordingCognito();
        var response = await new Auth(fake, "test-pool", "test-client")
            .Handler(Request("/reset", Values()), null!);

        Assert.Equal(200, response.StatusCode);
        Assert.Equal(" opaque session ", fake.Challenged!.Session);
        Assert.Equal(" leading and trailing ", fake.Challenged.ChallengeResponses["NEW_PASSWORD"]);
        Assert.Equal(1, fake.Calls);
    }
}
