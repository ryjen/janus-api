
using Amazon.Lambda.APIGatewayEvents;

namespace Janus;

using static Shared;

public partial class Generic
{
    public async Task<APIGatewayProxyResponse> Delete(DeleteRequest request)
    {
        try
        {
            await _db.Delete(request);
            return Response(200, new { Message = "Object deleted" });
        }
        catch (Exception ex)
        {
            return Response(401, new { Message = "Object delete failed", Error = ex.Message });
        }
    }
}
