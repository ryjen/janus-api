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

        try
        {
            var authResult = await _authenticationGateway.AuthenticateAsync(email, password);

            if (authResult.ChallengeName is not null)
                return Challenged(authResult.ChallengeName, authResult.Session);

            // Enforce this invariant at the application boundary as well:
            // a future injected gateway must not turn a missing token into HTTP 200.
            if (string.IsNullOrWhiteSpace(authResult.IdToken))
                throw new InvalidOperationException("Authentication gateway returned no token");

            return Response(OK, new { Token = authResult.IdToken });
        }
        catch (Exception error)
        {
            // Rejected credentials remain generic 401. Provider and unknown-effect
            // faults must not be mistaken for bad credentials.
            return AuthenticationFailed(error);
        }
    }
}
