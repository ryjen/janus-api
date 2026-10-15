#nullable enable

namespace Janus;

// Application decisions are independent of the API Gateway status/body format.
// A result without a usable token must never be reported as authenticated.
public enum AuthenticationOutcomeKind
{
    Authenticated,
    ChallengeRequired,
    Rejected,
    Unavailable
}

public sealed record AuthenticationOutcome(
    AuthenticationOutcomeKind Kind,
    string? Token = null,
    string? ChallengeName = null,
    string? Session = null)
{
    // C# record ToString normally prints all fields, including secrets.
    // Diagnostic string formatting must never include tokens or sessions.
    public override string ToString() => $"{nameof(AuthenticationOutcome)}({Kind})";
}

// Own Cognito-result interpretation and failure classification while keeping
// the gateway transport-independent and the HTTP adapter responsible for
// mapping application outcomes to existing v1 wire responses.
public sealed class AuthenticationService
{
    private readonly IAuthenticationGateway _gateway;

    public AuthenticationService(IAuthenticationGateway gateway) =>
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));

    public async Task<AuthenticationOutcome> AuthenticateAsync(string email, string password)
    {
        try
        {
            var result = await _gateway.AuthenticateAsync(email, password);
            if (result is null)
                return new(AuthenticationOutcomeKind.Unavailable);

            if (!string.IsNullOrWhiteSpace(result.ChallengeName))
                return new(AuthenticationOutcomeKind.ChallengeRequired,
                    ChallengeName: result.ChallengeName, Session: result.Session);

            if (!string.IsNullOrWhiteSpace(result.IdToken))
                return new(AuthenticationOutcomeKind.Authenticated, Token: result.IdToken);

            return new(AuthenticationOutcomeKind.Unavailable);
        }
        catch (Exception error)
        {
            return FromFailure(error);
        }
    }

    public async Task<AuthenticationOutcome> RespondToNewPasswordAsync(
        string email, string password, string session)
    {
        try
        {
            var token = await _gateway.RespondToNewPasswordAsync(email, password, session);
            return !string.IsNullOrWhiteSpace(token)
                ? new(AuthenticationOutcomeKind.Authenticated, Token: token)
                : new(AuthenticationOutcomeKind.Unavailable);
        }
        catch (Exception error)
        {
            return FromFailure(error);
        }
    }

    private static AuthenticationOutcome FromFailure(Exception error) =>
        CognitoAuthenticationErrorPolicy.Classify(error) == CognitoAuthenticationFailure.Rejected
            ? new(AuthenticationOutcomeKind.Rejected)
            : new(AuthenticationOutcomeKind.Unavailable);
}
