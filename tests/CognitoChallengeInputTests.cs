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

public sealed class CognitoChallengeInputTests
{
    private sealed class FakeCognito : AmazonCognitoIdentityProviderClient
    {
        public AdminInitiateAuthRequest? Login { get; private set; }
        public AdminRespondToAuthChallengeRequest? Challenge { get; private set; }
        public bool Fail { get; set; }

        public FakeCognito() : base(new AnonymousAWSCredentials(), RegionEndpoint.USEast1) { }

        public override Task<AdminInitiateAuthResponse> AdminInitiateAuthAsync(
            AdminInitiateAuthRequest request, CancellationToken cancellationToken = default)
        {
            Login = request;
            if (Fail) return Task.FromException<AdminInitiateAuthResponse>(
                new NotAuthorizedException("Credential rejected"));
            return Task.FromResult(new AdminInitiateAuthResponse
            {
                AuthenticationResult = new AuthenticationResultType { IdToken = "fake-login-token" }
            });
        }

        public override Task<AdminRespondToAuthChallengeResponse> AdminRespondToAuthChallengeAsync(
            AdminRespondToAuthChallengeRequest request, CancellationToken cancellationToken = default)
        {
            Challenge = request;
            if (Fail) return Task.FromException<AdminRespondToAuthChallengeResponse>(
                new NotAuthorizedException("Challenge rejected"));
            return Task.FromResult(new AdminRespondToAuthChallengeResponse
            {
                AuthenticationResult = new AuthenticationResultType { IdToken = "fake-challenge-token" }
            });
        }
    }

    [Theory]
    [InlineData("password")]
    [InlineData("session")]
    public void OpaqueInputsPreserveWhitespace(string field)
    {
        var input = new Dictionary<string, object> { [field] = " secret with spaces " };
        Assert.Equal(" secret with spaces ", AuthInput.ReadOpaque(input, field));
    }

    [Fact]
    public async Task AuthenticationRejectsStructuredPasswordBeforeAws()
    {
        var fake = new FakeCognito();
        var response = await new Auth(fake, "pool", "client").Authenticate(new()
        {
            ["email"] = "user@example.invalid", ["password"] = new { value = "bad" }
        });
        Assert.Equal(400, response.StatusCode);
        Assert.Null(fake.Login);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AuthenticationRejectsMissingEffectivePassword(string password)
    {
        var fake = new FakeCognito();
        var response = await new Auth(fake, "pool", "client").Authenticate(new()
        {
            ["email"] = "user@example.invalid", ["password"] = password
        });
        Assert.Equal(400, response.StatusCode);
        Assert.Null(fake.Login);
    }

    [Fact]
    public async Task AuthenticationPassesNormalizedEmailAndVerbatimPassword()
    {
        var fake = new FakeCognito();
        var response = await new Auth(fake, "pool", "client").Authenticate(new()
        {
            ["email"] = " user@example.invalid ", ["password"] = " password with spaces "
        });
        Assert.Equal(200, response.StatusCode);
        Assert.Equal("user@example.invalid", fake.Login!.AuthParameters["USERNAME"]);
        Assert.Equal(" password with spaces ", fake.Login.AuthParameters["PASSWORD"]);
        Assert.Equal("client", fake.Login.ClientId);
        Assert.Equal("pool", fake.Login.UserPoolId);
        Assert.DoesNotContain("password with spaces", response.Body);
    }

    [Fact]
    public async Task BadCredentialsRemainUnauthorizedWithoutEcho()
    {
        var fake = new FakeCognito { Fail = true };
        var response = await new Auth(fake, "pool", "client").Authenticate(new()
        {
            ["email"] = "user@example.invalid", ["password"] = "test-password"
        });
        Assert.Equal(401, response.StatusCode);
        Assert.DoesNotContain("Credential rejected", response.Body);
        Assert.DoesNotContain("test-password", response.Body);
    }

    [Fact]
    public async Task ChallengeRejectsNonStringSessionBeforeAws()
    {
        var fake = new FakeCognito();
        var response = await new Auth(fake, "pool", "client").NewPassword(new()
        {
            ["email"] = "user@example.invalid",
            ["password"] = "test-password",
            ["session"] = 42
        });
        Assert.Equal(400, response.StatusCode);
        Assert.Null(fake.Challenge);
    }

    [Fact]
    public async Task ChallengeRejectsNonStringPasswordBeforeAws()
    {
        var fake = new FakeCognito();
        var response = await new Auth(fake, "pool", "client").NewPassword(new()
        {
            ["email"] = "user@example.invalid",
            ["password"] = new[] { "not", "a", "password" },
            ["session"] = "test-session"
        });
        Assert.Equal(400, response.StatusCode);
        Assert.Null(fake.Challenge);
    }

    [Fact]
    public async Task ChallengeUsesVerbatimPasswordAndOpaqueSession()
    {
        var fake = new FakeCognito();
        var response = await new Auth(fake, "pool", "client").NewPassword(new()
        {
            ["email"] = " user@example.invalid ",
            ["password"] = " password with spaces ",
            ["session"] = " session with spaces "
        });
        Assert.Equal(200, response.StatusCode);
        Assert.Equal("user@example.invalid", fake.Challenge!.ChallengeResponses["USERNAME"]);
        Assert.Equal(" password with spaces ", fake.Challenge.ChallengeResponses["NEW_PASSWORD"]);
        Assert.Equal(" session with spaces ", fake.Challenge.Session);
        Assert.Equal(ChallengeNameType.NEW_PASSWORD_REQUIRED, fake.Challenge.ChallengeName);
    }

    [Fact]
    public async Task FailedChallengeRemainsUnauthorized()
    {
        var fake = new FakeCognito { Fail = true };
        var response = await new Auth(fake, "pool", "client").NewPassword(new()
        {
            ["email"] = "user@example.invalid",
            ["password"] = "test-password",
            ["session"] = "test-session"
        });
        Assert.Equal(401, response.StatusCode);
        Assert.DoesNotContain("Challenge rejected", response.Body);
    }

    [Theory]
    [InlineData("/authenticate", "password")]
    [InlineData("/authenticate", "email")]
    [InlineData("/reset", "password")]
    [InlineData("/reset", "session")]
    [InlineData("/reset", "email")]
    public async Task HttpAdapterRejectsStructuredCredentialBeforeCognito(string route, string key)
    {
        using var fake = new FakeCognito();
        var fields = new Dictionary<string, object>
        {
            ["email"] = "user@example.invalid",
            ["password"] = "sample password",
            ["session"] = "sample session"
        };
        fields[key] = new { nested = 42 };

        var response = await new Auth(fake, "pool", "client").Handler(
            new APIGatewayProxyRequest
            {
                Path = route,
                Body = JsonConvert.SerializeObject(fields)
            }, null!);

        Assert.Equal(400, response.StatusCode);
        Assert.Null(fake.Login);
        Assert.Null(fake.Challenge);
        Assert.DoesNotContain("nested", response.Body);
    }
}
