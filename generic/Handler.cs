
using Amazon.Lambda.APIGatewayEvents;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Janus;

using static Shared;

public partial class Generic
{
    private readonly Database _db = new Database();

    public async Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        switch (request.HttpMethod)
        {
        case "GET":
            return await Read(request.ParseForm<ReadRequest>());
        case "POST":
            return await Create(request.ParseJson<CreateRequest>());
        case "PUT":
            return await Update(request.ParseJson<UpdateRequest>());
        case "DELETE":
            return await Delete(request.ParseJson<DeleteRequest>());
        }

        return Response(500, new { Message = "Invalid request" });

    }
}
