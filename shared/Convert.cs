
using Amazon.Lambda.APIGatewayEvents;
using Newtonsoft.Json;

namespace Janus;

using Params = Dictionary<string, string>;

public static partial class Convert
{
    public static Params ToParams(this APIGatewayProxyRequest request)
    {
        return JsonConvert.DeserializeObject<Params>(request.Body);
    }

    public static string AuthToken(this APIGatewayProxyRequest request)
    {
        return request.Headers["Authorization"].Replace("Bearer ", "");
    }
}
