#nullable enable

using Janus;
using Xunit;

namespace Janus.Tests;

public sealed class AuthDiagnosticFormattingTests
{
    private const string SensitiveToken = "sensitive-test-id-token";
    private const string SensitiveSession = "sensitive-test-cognito-session";

    [Fact]
    public void SuccessfulAuthenticationOutcomeDoesNotPrintToken()
    {
        var outcome = new AuthenticationOutcome(
            AuthenticationOutcomeKind.Authenticated, Token: SensitiveToken);

        Assert.Equal("AuthenticationOutcome(Authenticated)", outcome.ToString());
        Assert.DoesNotContain(SensitiveToken, $"{outcome}");
    }

    [Fact]
    public void ChallengeOutcomeDoesNotPrintSessionOrToken()
    {
        var outcome = new AuthenticationOutcome(
            AuthenticationOutcomeKind.ChallengeRequired,
            Token: SensitiveToken,
            ChallengeName: "NEW_PASSWORD_REQUIRED",
            Session: SensitiveSession);

        Assert.Equal("AuthenticationOutcome(ChallengeRequired)", outcome.ToString());
        Assert.DoesNotContain(SensitiveToken, $"{outcome}");
        Assert.DoesNotContain(SensitiveSession, $"{outcome}");
    }

    [Fact]
    public void CognitoGatewayResultDoesNotPrintSensitiveFields()
    {
        var result = new LoginSessionResult(
            SensitiveToken, "NEW_PASSWORD_REQUIRED", SensitiveSession);

        Assert.Equal(nameof(LoginSessionResult), result.ToString());
        Assert.DoesNotContain(SensitiveToken, $"{result}");
        Assert.DoesNotContain(SensitiveSession, $"{result}");
        // This change must not strip actual credentials needed by the adapter.
        Assert.Equal(SensitiveToken, result.IdToken);
        Assert.Equal(SensitiveSession, result.Session);
    }
}
