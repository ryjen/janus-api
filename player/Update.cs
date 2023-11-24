
using Amazon.Lambda.APIGatewayEvents;

namespace Janus;

using RequestParams = Dictionary<string, string>;
using static Shared;
using PlayerModel = Janus.Model.Player;

public partial class Player
{
    public async Task<APIGatewayProxyResponse> Update(RequestParams request)
    {
        try
        {
            // TODO: parse right from json
            var account = new PlayerModel
            {
                Id = request["id"],
                Location = request["location"]
            };
            await _db.Save(account);
            return Response(200, new { Message = "Player updated" });
        }
        catch (Exception ex)
        {
            return Response(401, new { Message = "Player update failed", Error = ex.Message });
        }
    }
}
