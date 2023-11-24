
using Amazon.Lambda.APIGatewayEvents;

namespace Janus;

using AccountModel = Janus.Model.Account;

using static Shared;

public partial class Account
{
    public async Task<APIGatewayProxyResponse> Delete(string key)
    {
        try
        {
            await _db.Delete<AccountModel>(key);
            return Response(200, new { Message = "Account deleted" });
        }
        catch (Exception ex)
        {
            return Response(401, new { Message = "Account delete failed", Error = ex.Message });
        }
    }
}
