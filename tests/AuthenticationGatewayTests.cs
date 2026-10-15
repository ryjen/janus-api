#nullable enable

using Amazon;
using Amazon.Runtime;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Janus;
using Xunit;

namespace Janus.Tests;

public sealed class AuthenticationGatewayTests
{
    private sealed class FakeAuthenticationGateway : IAuthenticationGateway
    {
        public int LoginCalls { get; private set; }
        public int ChallengeCalls { get; private set; }
        public string? Email { get; private set; }
        public string? Password { get; private set; }
        public string? Session { get; private set; }
        public Exception? Failure { get; set; }
        public LoginSessionResult LoginResult { get; set; } =
            new("test-token", null, null);
        public string? ChallengeToken { get; set; } = "test-challenge-token";

        public Task<LoginSessionResult> AuthenticateAsync(
            string email, string password, CancellationToken cancellationToken = default)
        {
            LoginCalls++;
            Email = email;
            Password = password;
            return Failure is null ? Task.FromResult(LoginResult) :
                Task.FromException<LoginSessionResult>(Failure);
        }

        public Task<string?> RespondToNewPasswordAsync(
            string email, string password, string session,
            CancellationToken cancellationToken = default)
        {
            ChallengeCalls++;
            Email = email;
            Password = password;
            Session = session;
            return Failure is null ? Task.FromResult(ChallengeToken) :
                Task.FromException<string?>(Failure);
        }
    }

    private static Auth Handler(FakeAuthenticationGateway gateway)
    {
        var sdk = new AmazonCognitoIdentityProviderClient(
            new AnonymousAWSCredentials(), RegionEndpoint.USEast1);
        return new Auth(sdk, "test-pool", "test-client",
            enrollmentGateway: null, passwordRecoveryGateway: null,
            authenticationGateway: gateway);
    }

    private static Dictionary<string, object> LoginFields() => new()
    {
        ["email"] = " user@example.invalid ",
        ["password"] = " password with spaces "
    };

    private static Dictionary<string, object> ChallengeFields() => new()
    {
        ["email"] = " user@example.invalid ",
        ["password"] = " new password with spaces ",
        ["session"] = " session with spaces "
    };

    [Fact]
    public async Task LoginForwardsNormalizedEmailAndExactPassword()
    {
        var gateway = new FakeAuthenticationGateway();
        var response = await Handler(gateway).Authenticate(LoginFields());
        Assert.Equal(200, response.StatusCode);
        Assert.Contains("test-token", response.Body);
        Assert.Equal(1, gateway.LoginCalls);
        Assert.Equal("user@example.invalid", gateway.Email);
        Assert.Equal(" password with spaces ", gateway.Password);
        Assert.DoesNotContain("password with spaces", response.Body);
    }

    [Fact]
    public async Task MissingOrStructuredCredentialsNeverCallGateway()
    {
        var gateway = new FakeAuthenticationGateway();
        var fields = LoginFields();
        fields["password"] = new { value = "should-not-serialize" };
        var response = await Handler(gateway).Authenticate(fields);
        Assert.Equal(400, response.StatusCode);
        Assert.Equal(0, gateway.LoginCalls);
        Assert.DoesNotContain("should-not-serialize", response.Body);
    }

    [Fact]
    public async Task KnownChallengePreservesLegacyResponseAndOpaqueSession()
    {
        var gateway = new FakeAuthenticationGateway
        {
            LoginResult = new(null, ChallengeNameType.NEW_PASSWORD_REQUIRED.ToString(), " session token ")
        };
        var response = await Handler(gateway).Authenticate(LoginFields());
        Assert.Equal(401, response.StatusCode);
        Assert.Contains("New password required", response.Body);
        Assert.Contains(" session token ", response.Body);
        Assert.DoesNotContain("password with spaces", response.Body);
    }

    [Fact]
    public async Task UnrecognizedChallengePreservesLegacyError()
    {
        var gateway = new FakeAuthenticationGateway
        {
            LoginResult = new(null, "CUSTOM_CHALLENGE", "opaque-session")
        };
        var response = await Handler(gateway).Authenticate(LoginFields());
        Assert.Equal(401, response.StatusCode);
        Assert.Contains("Authorization incomplete", response.Body);
        Assert.Contains("CUSTOM_CHALLENGE", response.Body);
        Assert.Contains("opaque-session", response.Body);
    }

    [Fact]
    public async Task InvalidCredentialsStayGeneric401()
    {
        var gateway = new FakeAuthenticationGateway
        {
            Failure = new NotAuthorizedException("sensitive password denied")
        };
        var response = await Handler(gateway).Authenticate(LoginFields());
        Assert.Equal(401, response.StatusCode);
        Assert.DoesNotContain("sensitive", response.Body);
    }

    [Fact]
    public async Task UnexpectedProviderFailureIsGeneric503()
    {
        var gateway = new FakeAuthenticationGateway
        {
            Failure = new InternalErrorException("sensitive provider data")
        };
        var response = await Handler(gateway).Authenticate(LoginFields());
        Assert.Equal(503, response.StatusCode);
        Assert.DoesNotContain("sensitive", response.Body);
    }

    [Fact]
    public async Task ChallengeForwardsSessionAndPasswordVerbatim()
    {
        var gateway = new FakeAuthenticationGateway();
        var response = await Handler(gateway).NewPassword(ChallengeFields());
        Assert.Equal(200, response.StatusCode);
        Assert.Contains("test-challenge-token", response.Body);
        Assert.Equal(1, gateway.ChallengeCalls);
        Assert.Equal("user@example.invalid", gateway.Email);
        Assert.Equal(" new password with spaces ", gateway.Password);
        Assert.Equal(" session with spaces ", gateway.Session);
    }

    [Fact]
    public async Task NonStringSessionCannotTriggerChallenge()
    {
        var gateway = new FakeAuthenticationGateway();
        var fields = ChallengeFields();
        fields["session"] = new { token = "invalid" };
        var response = await Handler(gateway).NewPassword(fields);
        Assert.Equal(400, response.StatusCode);
        Assert.Equal(0, gateway.ChallengeCalls);
    }

    [Fact]
    public async Task ProviderFailureOnChallengeUsesExistingClassification()
    {
        var gateway = new FakeAuthenticationGateway
        {
            Failure = new InternalErrorException("private upstream failure")
        };
        var response = await Handler(gateway).NewPassword(ChallengeFields());
        Assert.Equal(503, response.StatusCode);
        Assert.DoesNotContain("private upstream failure", response.Body);
    }
}
