
using Amazon.Lambda.APIGatewayEvents;
using Amazon.CognitoIdentityProvider;
using static System.Net.HttpStatusCode;

namespace Janus;

using static Shared;

partial class Auth
{
    // Application outcomes are mapped to the legacy API Gateway wire format.
    // Validate invariants again here so an invalid future outcome is fail-closed.
    private static APIGatewayProxyResponse AuthenticationResponse(AuthenticationOutcome outcome)
    {
        if (outcome.Kind == AuthenticationOutcomeKind.Authenticated &&
            !string.IsNullOrWhiteSpace(outcome.Token))
            return Response(OK, new { Token = outcome.Token });

        if (outcome.Kind == AuthenticationOutcomeKind.ChallengeRequired &&
            !string.IsNullOrWhiteSpace(outcome.ChallengeName))
            return Challenged(outcome.ChallengeName, outcome.Session!);

        var status = outcome.Kind == AuthenticationOutcomeKind.Rejected
            ? Unauthorized : ServiceUnavailable;
        return Response(status, new { Message = "Authentication failed" });
    }

    protected static APIGatewayProxyResponse Challenged(string type, string session)
    {
        // Preserve the legacy response for known and unrecognized Cognito
        // challenges without leaking SDK response types through the port.
        if (string.Equals(type, ChallengeNameType.NEW_PASSWORD_REQUIRED.ToString(),
                StringComparison.Ordinal))
        {
            return Response(Unauthorized, new
            {
                Message = "Authentication incomplete",
                Error = "New password required",
                Session = session
            });
        }

        return Response(Unauthorized, new
        {
            Message = "Authorization incomplete",
            Error = type,
            Session = session
        });
    }
}
