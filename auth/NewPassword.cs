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
        var email = PasswordRecoveryInput.Read(request, "email");
        var password = AuthInput.ReadOpaque(request, "password");
        var session = AuthInput.ReadOpaque(request, "session");
        if (email is null || password is null || session is null)
            return Response(BadRequest, new { Message = "Email, new password and challenge session required" });

        var outcome = await _authenticationService.RespondToNewPasswordAsync(
            email, password, session);
        return AuthenticationResponse(outcome);
    }
}
