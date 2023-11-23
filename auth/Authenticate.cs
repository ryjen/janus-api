
using Amazon.Lambda.APIGatewayEvents;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;

namespace Janus;

using static Shared;
using RequestParams = Dictionary<string, string>;

public partial class Auth
{
    public async Task<APIGatewayProxyResponse> Authenticate(RequestParams request)
    {
        try
        {
            string email = request["email"];
            string password = request["password"];

            var authRequest = new AdminInitiateAuthRequest
            {
                UserPoolId = userPoolId,
                ClientId = clientId,
                AuthFlow = AuthFlowType.ADMIN_NO_SRP_AUTH,
                AuthParameters = new Dictionary<string, string>
                {
                    {"USERNAME", email},
                    {"PASSWORD", password}
                }
            };
            var authResponse = await _cognitoClient.AdminInitiateAuthAsync(authRequest);

            log(authResponse);

            if (authResponse.ChallengeName != null)
            {
                return Challenged(authResponse.ChallengeName, authResponse.Session);
            }

            string token = authResponse.AuthenticationResult.AccessToken;

            return Response(200, new { Token = token });
        }
        catch (Exception ex)
        {
            return Response(401, new { Message = "Authentication failed", Error = ex.Message });
        }
    }
}
