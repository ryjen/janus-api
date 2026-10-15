#nullable enable

using Amazon.CognitoIdentityProvider.Model;
using Janus;
using Xunit;

namespace Janus.Tests;

public sealed class AuthenticationServiceTests
{
    private sealed class FakeGateway : IAuthenticationGateway
    {
        public LoginSessionResult? LoginResult { get; set; } = new("sample-token", null, null);
        public string? ChallengeToken { get; set; } = "challenge-token";
        public Exception? LoginFailure { get; set; }
        public Exception? ChallengeFailure { get; set; }
        public int LoginCalls { get; private set; }
        public int ChallengeCalls { get; private set; }
        public string? LastPassword { get; private set; }
        public string? LastSession { get; private set; }

        public Task<LoginSessionResult> AuthenticateAsync(
            string email, string password, CancellationToken cancellationToken = default)
        {
            LoginCalls++;
            LastPassword = password;
            return LoginFailure is not null
                ? Task.FromException<LoginSessionResult>(LoginFailure)
                : Task.FromResult(LoginResult!);
        }

        public Task<string?> RespondToNewPasswordAsync(
            string email, string password, string session,
            CancellationToken cancellationToken = default)
        {
            ChallengeCalls++;
            LastPassword = password;
            LastSession = session;
            return ChallengeFailure is not null
                ? Task.FromException<string?>(ChallengeFailure)
                : Task.FromResult(ChallengeToken);
        }
    }

    [Fact]
    public async Task TokenResultIsAuthenticatedWithoutTransportInformation()
    {
        var outcome = await new AuthenticationService(new FakeGateway())
            .AuthenticateAsync("user@example.invalid", "opaque password");

        Assert.Equal(AuthenticationOutcomeKind.Authenticated, outcome.Kind);
        Assert.Equal("sample-token", outcome.Token);
        Assert.Null(outcome.ChallengeName);
    }

    [Fact]
    public async Task CognitoChallengeIsApplicationOutcomeNotHttpStatus()
    {
        var fake = new FakeGateway
        {
            LoginResult = new(null, "NEW_PASSWORD_REQUIRED", "opaque session")
        };
        var outcome = await new AuthenticationService(fake)
            .AuthenticateAsync("user@example.invalid", "password");

        Assert.Equal(AuthenticationOutcomeKind.ChallengeRequired, outcome.Kind);
        Assert.Equal("NEW_PASSWORD_REQUIRED", outcome.ChallengeName);
        Assert.Equal("opaque session", outcome.Session);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyTokenCannotBecomeSuccessfulLogin(string? token)
    {
        var outcome = await new AuthenticationService(
            new FakeGateway { LoginResult = new(token, null, null) })
            .AuthenticateAsync("user@example.invalid", "password");

        Assert.Equal(AuthenticationOutcomeKind.Unavailable, outcome.Kind);
        Assert.Null(outcome.Token);
    }

    [Fact]
    public async Task NullLoginResultFailsClosed()
    {
        var outcome = await new AuthenticationService(
            new FakeGateway { LoginResult = null })
            .AuthenticateAsync("user@example.invalid", "password");

        Assert.Equal(AuthenticationOutcomeKind.Unavailable, outcome.Kind);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task EmptyChallengeTokenCannotBecomeSuccess(string? token)
    {
        var outcome = await new AuthenticationService(
            new FakeGateway { ChallengeToken = token })
            .RespondToNewPasswordAsync("user@example.invalid", " opaque password ", " opaque session ");

        Assert.Equal(AuthenticationOutcomeKind.Unavailable, outcome.Kind);
        Assert.Null(outcome.Token);
    }

    [Fact]
    public async Task ChallengeForwardsOpaqueSecretsWithoutNormalization()
    {
        var fake = new FakeGateway();
        var outcome = await new AuthenticationService(fake)
            .RespondToNewPasswordAsync("user@example.invalid", " opaque password ", " opaque session ");

        Assert.Equal(AuthenticationOutcomeKind.Authenticated, outcome.Kind);
        Assert.Equal("challenge-token", outcome.Token);
        Assert.Equal(" opaque password ", fake.LastPassword);
        Assert.Equal(" opaque session ", fake.LastSession);
    }

    [Fact]
    public async Task RejectedLoginProducesTypedRejection()
    {
        var fake = new FakeGateway
        {
            LoginFailure = new NotAuthorizedException("secret upstream message")
        };
        var outcome = await new AuthenticationService(fake)
            .AuthenticateAsync("user@example.invalid", "secret password");

        Assert.Equal(AuthenticationOutcomeKind.Rejected, outcome.Kind);
        Assert.Null(outcome.Token);
    }

    [Fact]
    public async Task UnknownLoginFailureProducesUnavailableWithoutExceptionDetail()
    {
        var fake = new FakeGateway
        {
            LoginFailure = new InvalidOperationException("sensitive infrastructure detail")
        };
        var outcome = await new AuthenticationService(fake)
            .AuthenticateAsync("user@example.invalid", "secret password");

        Assert.Equal(AuthenticationOutcomeKind.Unavailable, outcome.Kind);
        Assert.Null(outcome.Token);
        Assert.Null(outcome.ChallengeName);
    }

    [Fact]
    public async Task KnownChallengeRejectionProducesTypedRejection()
    {
        var fake = new FakeGateway
        {
            ChallengeFailure = new NotAuthorizedException("bad session")
        };
        var outcome = await new AuthenticationService(fake)
            .RespondToNewPasswordAsync("user@example.invalid", "password", "session");

        Assert.Equal(AuthenticationOutcomeKind.Rejected, outcome.Kind);
    }

    [Fact]
    public async Task UnknownChallengeFailureProducesUnavailable()
    {
        var fake = new FakeGateway
        {
            ChallengeFailure = new InvalidOperationException("private provider fault")
        };
        var outcome = await new AuthenticationService(fake)
            .RespondToNewPasswordAsync("user@example.invalid", "password", "session");

        Assert.Equal(AuthenticationOutcomeKind.Unavailable, outcome.Kind);
    }

    [Fact]
    public void ConstructorRejectsMissingGateway()
    {
        Assert.Throws<ArgumentNullException>(() => new AuthenticationService(null!));
    }
}
