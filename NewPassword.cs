using Amazon.Lambda.APIGatewayEvents;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;

namespace Janus;

using RequestParams = Dictionary<string, string>;

public partial class Auth
{

    public async Task<APIGatewayProxyResponse> NewPassword(RequestParams request)
    {
        try
        {
            string email = request["email"];
            string password = request["password"];
            string session = request["session"];

            var newPasswordRequest = new AdminRespondToAuthChallengeRequest
            {
                ChallengeName = ChallengeNameType.NEW_PASSWORD_REQUIRED,
                ClientId = clientId,
                UserPoolId = userPoolId,
                ChallengeResponses = new Dictionary<string, string> {
                    {"USERNAME", email},
                    {"NEW_PASSWORD", password}
                },
                Session = session
            };
            var authResponse = await _cognitoClient.AdminRespondToAuthChallengeAsync(newPasswordRequest);

            log(authResponse);

            string token = authResponse.AuthenticationResult.IdToken;

            return Response(200, new { Token = token });
        }
        catch (Exception ex)
        {
            return Response(401, new { Message = "Authentication failed", Error = ex.Message });
        }
    }
}
