
using Amazon.Lambda.APIGatewayEvents;
using Amazon.CognitoIdentityProvider;
using Newtonsoft.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Janus;

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
            return await Authenticate(requestParams(request));
        case "/reset":
            return await NewPassword(requestParams(request));
        case "/signup":
            return await SignUp(requestParams(request));
        case "/account":
            return await Account(authToken(request));
        default:
            return Response(401, new { Message = "Authentication failed", Error = "unknown path" });
        }
    }

    public void log(Object obj)
    {
        System.Console.WriteLine("{0}", JsonConvert.SerializeObject(obj));
    }

    private RequestParams requestParams(APIGatewayProxyRequest request)
    {

        return JsonConvert.DeserializeObject<RequestParams>(request.Body);
    }

    private string authToken(APIGatewayProxyRequest request)
    {
        return request.Headers["Authorization"].Replace("Bearer ", "");
    }
}
