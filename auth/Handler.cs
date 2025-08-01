
using Amazon.Lambda.APIGatewayEvents;
using Amazon.CognitoIdentityProvider;
using static System.Net.HttpStatusCode;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Janus;

using static Shared;

public partial class Auth
{

    private readonly AmazonCognitoIdentityProviderClient _cognitoClient = new AmazonCognitoIdentityProviderClient();

    private readonly string _userPoolId = Environment.GetEnvironmentVariable("USER_POOL_ID");
    private readonly string _clientId = Environment.GetEnvironmentVariable("CLIENT_ID");

    public async Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            switch (request.Path)
            {
            case "/authenticate":
                return await Authenticate(request.Body.ToParams());
            case "/reset":
                return await NewPassword(request.Body.ToParams());
            case "/signup":
                return await SignUp(request.Body.ToParams());
            default:
                return Response(Unauthorized, new { Message = "Authentication failed", Error = "unknown path" });
            }
        }
        catch
        {
            return Response(InternalServerError, new { Message = "Internal error" });
        }
    }
}
