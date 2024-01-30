
using Amazon.Lambda.APIGatewayEvents;

namespace Janus;

using static Shared;

public partial class Generic
{
    public async Task<APIGatewayProxyResponse> Read(ReadRequest request)
    {
        try
        {
            var obj = await _db.Read(request);
            return Response(200, obj.ToJson());
        }
        catch (Exception ex)
        {
            return Response(401, new { Message = "Object read failed", Error = ex.Message });
        }
    }
}
