
using Amazon.Lambda.APIGatewayEvents;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using static System.Net.HttpStatusCode;

namespace Janus;

using static Shared;
using RequestParams = Dictionary<string, object>;

public partial class Auth
{
    public async Task<APIGatewayProxyResponse> Authenticate(RequestParams request)
    {
        try
        {
            string email = request["email"].ToString();
            string password = request["password"].ToString();

            var authRequest = new AdminInitiateAuthRequest
            {
                UserPoolId = _userPoolId,
                ClientId = _clientId,
                AuthFlow = AuthFlowType.ADMIN_NO_SRP_AUTH,
                AuthParameters = new Dictionary<string, string>
                {
                    {"USERNAME", email},
                    {"PASSWORD", password}
                }
            };
            var authResponse = await _cognitoClient.AdminInitiateAuthAsync(authRequest);

            if (authResponse.ChallengeName != null)
            {
                return Challenged(authResponse.ChallengeName, authResponse.Session);
            }

            string token = authResponse.AuthenticationResult.AccessToken;

            return Response(OK, new { Token = token });
        }
        catch
        {
            return Response(Unauthorized, new { Message = "Authentication failed" });
        }
    }
}
