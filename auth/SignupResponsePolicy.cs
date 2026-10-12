using Amazon.Lambda.APIGatewayEvents;
using static System.Net.HttpStatusCode;

namespace Janus;

// Never reveal Cognito's UsernameExistsException to an unauthenticated caller.
// HTTP response equivalence is not a guarantee against timing-side channels.
public static class SignupResponsePolicy
{
    public static APIGatewayProxyResponse Acknowledged() =>
        Shared.Response(OK, new { Message = "If eligible, a confirmation code will be sent" });
}
