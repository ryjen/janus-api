using Amazon.Lambda.APIGatewayEvents;
using Amazon.CognitoIdentityProvider.Model;
using static System.Net.HttpStatusCode;

namespace Janus;

using static Shared;
using RequestParams = Dictionary<string, object>;

public partial class Auth
{
    public async Task<APIGatewayProxyResponse> NewPassword(RequestParams request)
    {
        var email = PasswordRecoveryInput.Read(request, "email");
        var password = AuthInput.ReadOpaque(request, "password");
        var session = AuthInput.ReadOpaque(request, "session");
        if (email is null || password is null || session is null)
            return Response(BadRequest, new { Message = "Email, new password and challenge session required" });

        try
        {
            var authResponse = await _cognitoClient.AdminRespondToAuthChallengeAsync(new AdminRespondToAuthChallengeRequest
            {
                ChallengeName = ChallengeNameType.NEW_PASSWORD_REQUIRED,
                ClientId = _clientId,
                UserPoolId = _userPoolId,
                ChallengeResponses = new Dictionary<string, string>
                {
                    { "USERNAME", email },
                    { "NEW_PASSWORD", password }
                },
                Session = session
            });

            return Response(OK, new { Token = authResponse.AuthenticationResult.IdToken });
        }
        catch
        {
            return Response(Unauthorized, new { Message = "Authentication failed" });
        }
    }
}
