
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
        var requestParams = JsonConvert.DeserializeObject<RequestParams>(request.Body);

        switch (request.Path)
        {
        case "/authenticate":
            return await Authenticate(requestParams);
        case "/reset":
            return await NewPassword(requestParams);
        default:
            return Response(401, new { Message = "Authentication failed", Error = "unknown path" });
        }
    }

    public void log(Object obj)
    {
        System.Console.WriteLine("{0}", JsonConvert.SerializeObject(obj));
    }
}
