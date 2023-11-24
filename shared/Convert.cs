
using Amazon.Lambda.APIGatewayEvents;
using System.IdentityModel.Tokens.Jwt;
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
        var auth = request.Headers["Authorization"].Replace("Bearer ", "");
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(auth);

        Shared.log(token);

        return token.Payload.Sub;
    }
}
