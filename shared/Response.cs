
using Amazon.Lambda.APIGatewayEvents;

namespace Janus;

public partial class Shared
{
    public static APIGatewayProxyResponse Response(int status, object body)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = status,
            Body = body.JsonSerialize(),
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
        };
    }
}
