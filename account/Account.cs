
using Amazon.Lambda.APIGatewayEvents;
using Amazon.CognitoIdentityProvider.Model;

namespace Janus;

using static Shared;

public partial class Account
{
    public async Task<APIGatewayProxyResponse> ReadAccount(string token)
    {
        try
        {
            var getUserRequest = new GetUserRequest
            {
                AccessToken = token
            };
            var response = await _cognitoClient.GetUserAsync(getUserRequest);

            log(response);

            return Response(200, new { Message = "testing" });
            // TODO: handle errors and rollback
            //var account = await GetAccountByEmail(response.Username);

            // TODO: use models
            //return Response(200, account);
        }
        catch (Exception ex)
        {
            return Response(401, new { Message = "Authentication failed", Error = ex.Message });
        }
    }
}
