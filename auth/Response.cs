
using Amazon.Lambda.APIGatewayEvents;
using Amazon.CognitoIdentityProvider;
using static System.Net.HttpStatusCode;

namespace Janus;

using static Shared;

partial class Auth
{
    // All login/challenge errors use the same generic body; status reflects
    // known rejected credentials versus an indeterminate provider failure.
    private static APIGatewayProxyResponse AuthenticationFailed(Exception error)
    {
        var kind = CognitoAuthenticationErrorPolicy.Classify(error);
        var status = kind == CognitoAuthenticationFailure.Rejected
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
