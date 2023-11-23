
using Amazon.Lambda.APIGatewayEvents;
using Amazon.CognitoIdentityProvider;
using Newtonsoft.Json;

namespace Janus;

using static Shared;

public partial class Account
{
    public async Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        switch(request.HttpMethod)
        {
        case "GET":
            return await ReadAccount(request.AuthToken());
        case "POST":
            break;
        case "PUT":
            break;
        case "DELETE":
            break;
        }
    }

}
