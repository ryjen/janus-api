
using Amazon.Lambda.APIGatewayEvents;

namespace Janus;

using static Shared;
using AccountModel = Janus.Model.Account;

public partial class Account
{
    public async Task<APIGatewayProxyResponse> Read(string key)
    {
        try
        {
            var account = await _db.Load<AccountModel>(key);
            return Response(200, account);
        }
        catch (Exception ex)
        {
            return Response(401, new { Message = "Authentication failed", Error = ex.Message });
        }
    }
}
