
using Amazon.Lambda.APIGatewayEvents;
using Amazon.CognitoIdentityProvider;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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
            return Response(300, new { Message = "Authentication incomplete", Error = errors[type], Session = session });
        }
        else
        {
            return Response(400, new { Message = "Authorization incomplete", Error = type.ToString(), Session = session });
        }
    }
}
