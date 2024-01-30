
using Amazon.Lambda.APIGatewayEvents;
using System.Net;

namespace Janus;

public partial class Shared
{
    public static APIGatewayProxyResponse Response(HttpStatusCode status, object body)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = (int)status,
            Body = body.JsonSerialize(),
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
        };
    }
}
