using Amazon.Lambda.APIGatewayEvents;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using static System.Net.HttpStatusCode;

namespace Janus;

using static Shared;
using RequestParams = Dictionary<string, object>;

public partial class Auth
{

    public async Task<APIGatewayProxyResponse> NewPassword(RequestParams request)
    {
        try
        {
            string email = request["email"].ToString();
            string password = request["password"].ToString();
            string session = request["session"].ToString();

            var newPasswordRequest = new AdminRespondToAuthChallengeRequest
            {
                ChallengeName = ChallengeNameType.NEW_PASSWORD_REQUIRED,
                ClientId = _clientId,
                UserPoolId = _userPoolId,
                ChallengeResponses = new Dictionary<string, string> {
                    {"USERNAME", email},
                    {"NEW_PASSWORD", password}
                },
                Session = session
            };
            var authResponse = await _cognitoClient.AdminRespondToAuthChallengeAsync(newPasswordRequest);

            string token = authResponse.AuthenticationResult.AccessToken;

            return Response(OK, new { Token = token });
        }
        catch
        {
            return Response(Unauthorized, new { Message = "Authentication failed" });
        }
    }
}
