
using Amazon.Lambda.APIGatewayEvents;

namespace Janus;

using static Shared;

public partial class Generic
{
    public async Task<APIGatewayProxyResponse> Create(CreateRequest request)
    {
        try
        {
            await _db.Create(request);
            return Response(200, new { Message = "Object created" });
        }
        catch (Exception ex)
        {
            return Response(401, new { Message = "Object create failed", Error = ex.Message });
        }
    }
}
