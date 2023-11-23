
using Amazon.Lambda.APIGatewayEvents;
using Amazon.CognitoIdentityProvider;
using Newtonsoft.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Janus;

using static Shared;
using RequestParams = Dictionary<string, string>;

public partial class Auth
{

    private readonly AmazonCognitoIdentityProviderClient _cognitoClient = new AmazonCognitoIdentityProviderClient();

    private readonly string userPoolId = Environment.GetEnvironmentVariable("USER_POOL_ID");
    private readonly string clientId = Environment.GetEnvironmentVariable("CLIENT_ID");

    public async Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
    {

        switch (request.Path)
        {
        case "/authenticate":
            return await Authenticate(request.ToParams());
        case "/reset":
            return await NewPassword(request.ToParams());
        case "/signup":
            return await SignUp(request.ToParams());
        default:
            return Response(401, new { Message = "Authentication failed", Error = "unknown path" });
        }
    }
}
