#nullable enable

using Amazon;
using Amazon.Runtime;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Janus;
using Xunit;

namespace Janus.Tests;

public sealed class CognitoAuthenticationGatewayAdapterTests
{
    private sealed class FakeCognito : AmazonCognitoIdentityProviderClient
    {
        public AdminInitiateAuthRequest? LoginRequest { get; private set; }
        public AdminRespondToAuthChallengeRequest? ChallengeRequest { get; private set; }
        public AdminInitiateAuthResponse LoginResponse { get; set; } = new()
        {
            AuthenticationResult = new AuthenticationResultType { IdToken = "test-token" }
        };
        public AdminRespondToAuthChallengeResponse ChallengeResponse { get; set; } = new()
        {
            AuthenticationResult = new AuthenticationResultType { IdToken = "test-token" }
        };

        public FakeCognito() : base(new AnonymousAWSCredentials(), RegionEndpoint.USEast1) { }

        public override Task<AdminInitiateAuthResponse> AdminInitiateAuthAsync(
            AdminInitiateAuthRequest request, CancellationToken cancellationToken = default)
        {
            LoginRequest = request;
            return Task.FromResult(LoginResponse);
        }

        public override Task<AdminRespondToAuthChallengeResponse> AdminRespondToAuthChallengeAsync(
            AdminRespondToAuthChallengeRequest request, CancellationToken cancellationToken = default)
        {
            ChallengeRequest = request;
            return Task.FromResult(ChallengeResponse);
        }
    }

    [Fact]
    public async Task LoginAdapterPreservesSdkFlowAndCredentialValues()
    {
        using var sdk = new FakeCognito();
        var gateway = new CognitoAuthenticationGateway(sdk, "test-pool", "test-client");
        var result = await gateway.AuthenticateAsync("user@example.invalid", " password with spaces ");

        Assert.Equal("test-token", result.IdToken);
        Assert.Null(result.ChallengeName);
        Assert.Equal("test-pool", sdk.LoginRequest!.UserPoolId);
        Assert.Equal("test-client", sdk.LoginRequest.ClientId);
        Assert.Equal(AuthFlowType.ADMIN_USER_PASSWORD_AUTH, sdk.LoginRequest.AuthFlow);
        Assert.Equal("user@example.invalid", sdk.LoginRequest.AuthParameters["USERNAME"]);
        Assert.Equal(" password with spaces ", sdk.LoginRequest.AuthParameters["PASSWORD"]);
    }

    [Fact]
    public async Task CognitoChallengeIsMappedWithoutLeakingSdkResponse()
    {
        using var sdk = new FakeCognito
        {
            LoginResponse = new AdminInitiateAuthResponse
            {
                ChallengeName = ChallengeNameType.NEW_PASSWORD_REQUIRED,
                Session = "opaque-session"
            }
        };
        var result = await new CognitoAuthenticationGateway(sdk, "pool", "client")
            .AuthenticateAsync("user@example.invalid", "password");
        Assert.Null(result.IdToken);
        Assert.Equal(ChallengeNameType.NEW_PASSWORD_REQUIRED.ToString(), result.ChallengeName);
        Assert.Equal("opaque-session", result.Session);
    }

    [Fact]
    public async Task ChallengeAdapterUsesExpectedSdkFlowAndPreservesOpaqueValues()
    {
        using var sdk = new FakeCognito();
        var gateway = new CognitoAuthenticationGateway(sdk, "pool", "client");
        var result = await gateway.RespondToNewPasswordAsync(
            "user@example.invalid", " new password ", " session ");
        Assert.Equal("test-token", result);
        Assert.Equal(ChallengeNameType.NEW_PASSWORD_REQUIRED, sdk.ChallengeRequest!.ChallengeName);
        Assert.Equal("pool", sdk.ChallengeRequest.UserPoolId);
        Assert.Equal("client", sdk.ChallengeRequest.ClientId);
        Assert.Equal("user@example.invalid", sdk.ChallengeRequest.ChallengeResponses["USERNAME"]);
        Assert.Equal(" new password ", sdk.ChallengeRequest.ChallengeResponses["NEW_PASSWORD"]);
        Assert.Equal(" session ", sdk.ChallengeRequest.Session);
    }

    [Fact]
    public async Task TokenlessCognitoSuccessFailsClosed()
    {
        using var sdk = new FakeCognito
        {
            LoginResponse = new AdminInitiateAuthResponse(),
            ChallengeResponse = new AdminRespondToAuthChallengeResponse()
        };
        var gateway = new CognitoAuthenticationGateway(sdk, "pool", "client");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => gateway.AuthenticateAsync("user@example.invalid", "password"));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => gateway.RespondToNewPasswordAsync("user@example.invalid", "password", "session"));
    }
}
