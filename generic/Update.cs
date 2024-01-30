
using Amazon.Lambda.APIGatewayEvents;

namespace Janus;

using static Shared;

public partial class Generic
{
    public async Task<APIGatewayProxyResponse> Update(UpdateRequest request)
    {
        try
        {
            await _db.Update(request);
            return Response(200, new { Message = "Object updated" });
        }
        catch (Exception ex)
        {
            return Response(401, new { Message = "Object update failed", Error = ex.Message });
        }
    }
}
