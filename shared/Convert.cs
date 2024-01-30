
using Amazon.Lambda.APIGatewayEvents;
using System.IdentityModel.Tokens.Jwt;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Janus;

using Params = Dictionary<string, object>;

public static partial class Convert
{
    public static string JsonSerialize(this object body)
    {
        if (body is string) return body as string;

        var serializerSettings = new JsonSerializerSettings();
        serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        return JsonConvert.SerializeObject(body, serializerSettings);
    }

    public static T JsonDeserialize<T>(this string body)
    {
        var serializerSettings = new JsonSerializerSettings();
        serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        return JsonConvert.DeserializeObject<T>(body, serializerSettings);
    }

public static T ParseJson<T>(this APIGatewayProxyRequest request) where T :
    Request
    {
        return request.Body.JsonDeserialize<T>();
    }

public static T ParseForm<T>(this APIGatewayProxyRequest request) where T :
    Request
    {
        var json = request.QueryStringParameters.JsonSerialize();
        return json.JsonDeserialize<T>();
    }
    public static Params ToParams(this APIGatewayProxyRequest request)
    {
        return JsonDeserialize<Params>(request.Body);
    }

    public static string AuthorizedId(this APIGatewayProxyRequest request)
    {
        var auth = request.Headers["Authorization"].Replace("Bearer ", "");
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(auth);

        Shared.log(token);

        return token.Payload.Sub;
    }
}
