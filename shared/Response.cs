
using Amazon.Lambda.APIGatewayEvents;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

public partial class Shared
{
    public static APIGatewayProxyResponse Response(int status, Object body)
    {
        var serializerSettings = new JsonSerializerSettings();
        serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        var responseBody = JsonConvert.SerializeObject(body, serializerSettings);

        return new APIGatewayProxyResponse
        {
            StatusCode = status,
            Body = responseBody,
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
        };
    }
}
