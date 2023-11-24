
using Amazon.Lambda.APIGatewayEvents;

namespace Janus;

using RequestParams = Dictionary<string, string>;
using static Shared;
using AccountModel = Janus.Model.Account;

public partial class Account
{
    public async Task<APIGatewayProxyResponse> Update(RequestParams request)
    {
        try
        {
            var account = new AccountModel
            {
                Id = request["id"],
                Email = request["email"]
            };
            await _db.Save(account);
            return Response(200, new { Message = "Account updated" });
        }
        catch (Exception ex)
        {
            return Response(401, new { Message = "Account update failed", Error = ex.Message });
        }
    }
}
