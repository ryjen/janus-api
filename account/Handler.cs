
using Amazon.Lambda.APIGatewayEvents;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Janus;

using static Shared;

public partial class Account
{
    private readonly Database _db = new Database();

    public async Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        switch (request.HttpMethod)
        {
        case "GET":
            return await Read(request.AuthToken());
        case "POST":
        case "PUT":
            return await Update(request.ToParams());
        case "DELETE":
            return await Delete(request.AuthToken());

        }

        return Response(500, new { Message = "Invalid request" });

    }
}
