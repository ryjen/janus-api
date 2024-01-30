
using Amazon.Lambda.APIGatewayEvents;
using static System.Net.HttpStatusCode;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Janus;

using static Shared;

public partial class Data : DataHandler
{
    public override async Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
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

            return Response(BadRequest, new { Message = "Invalid request" });
        }
        catch
        {
            return Response(InternalServerError, new { Message = "Internal error" });

        }
    }
}
