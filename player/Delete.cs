
using Amazon.Lambda.APIGatewayEvents;

namespace Janus;

using PlayerModel = Janus.Model.Player;

using static Shared;

public partial class Player
{
    public async Task<APIGatewayProxyResponse> Delete(string key)
    {
        try
        {
            await _db.Delete<PlayerModel>(key);
            return Response(200, new { Message = "Player deleted" });
        }
        catch (Exception ex)
        {
            return Response(401, new { Message = "Player delete failed", Error = ex.Message });
        }
    }
}
