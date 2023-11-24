
using Amazon.Lambda.APIGatewayEvents;

namespace Janus;

using static Shared;
using PlayerModel = Janus.Model.Player;

public partial class Player
{
    public async Task<APIGatewayProxyResponse> Read(string key)
    {
        try
        {
            var account = await _db.Load<PlayerModel>(key);
            return Response(200, account);
        }
        catch (Exception ex)
        {
            return Response(401, new { Message = "Player load failed", Error = ex.Message });
        }
    }
}
