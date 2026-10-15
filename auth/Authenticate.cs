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
        // Distinguish malformed input from invalid credentials. A structured,
        // numeric, or missing password must never be coerced to a string.
        var email = PasswordRecoveryInput.Read(request, "email");
        var password = AuthInput.ReadOpaque(request, "password");
        if (email is null || password is null)
            return Response(BadRequest, new { Message = "Email and password required" });

        var outcome = await _authenticationService.AuthenticateAsync(email, password);
        return AuthenticationResponse(outcome);
    }
}
