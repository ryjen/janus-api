
using Amazon.Lambda.APIGatewayEvents;
using Amazon.CognitoIdentityProvider;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Janus;

partial class Auth
{
    protected static APIGatewayProxyResponse Response(int status, Object body)
    {
        var serializerSettings = new JsonSerializerSettings();
        serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        var responseBody = JsonConvert.SerializeObject(body, serializerSettings);

        return new APIGatewayProxyResponse
        {
            StatusCode = status,
            Body = responseBody,
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
        };
    }

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
