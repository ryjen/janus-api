
using Amazon.Lambda.APIGatewayEvents;
using Amazon.CognitoIdentityProvider;
using static System.Net.HttpStatusCode;

namespace Janus;

using static Shared;

partial class Auth
{
    protected static APIGatewayProxyResponse Challenged(ChallengeNameType type, string session)
    {
        var errors = new Dictionary<ChallengeNameType, string>
        {
            { ChallengeNameType.NEW_PASSWORD_REQUIRED, "New password required"}
        };

        if (errors.ContainsKey(type))
        {
            return Response(Unauthorized, new { Message = "Authentication incomplete", Error = errors[type], Session = session });
        }
        else
        {
            return Response(Unauthorized, new { Message = "Authorization incomplete", Error = type.ToString(), Session = session });
        }
    }
}
