using Amazon.Lambda.APIGatewayEvents;
using Amazon.CognitoIdentityProvider.Model;
using static System.Net.HttpStatusCode;

namespace Janus;

using static Shared;
using RequestParams = Dictionary<string, object>;

public partial class Auth
{
    // A request must not reveal whether the email has a Cognito account.
    public async Task<APIGatewayProxyResponse> StartPasswordRecovery(RequestParams request)
    {
        var email = PasswordRecoveryInput.Read(request, "email");
        if (email is null)
            return Response(BadRequest, new { Message = "Email required" });

        try
        {
            await _cognitoClient.ForgotPasswordAsync(new ForgotPasswordRequest
            {
                ClientId = _clientId,
                Username = email
            });
        }
        catch (UserNotFoundException) { /* Maintain a generic response. */ }
        catch (InvalidParameterException) { /* Cognito may simulate a missing-user recovery error. */ }
        catch
        {
            return Response(ServiceUnavailable, new { Message = "Password recovery unavailable" });
        }

        return Response(Accepted, new { Message = "If eligible, a recovery code will be sent" });
    }

    // Cognito verifies the email code and enforces the user-pool password policy.
    // Successful recovery never issues or accepts an unverified application token.
    public async Task<APIGatewayProxyResponse> ConfirmPasswordRecovery(RequestParams request)
    {
        var email = PasswordRecoveryInput.Read(request, "email");
        var code = PasswordRecoveryInput.Read(request, "code");
        var password = PasswordRecoveryInput.Read(request, "password");

        if (email is null || code is null || password is null)
            return Response(BadRequest, new { Message = "Email, confirmation code and new password required" });

        try
        {
            await _cognitoClient.ConfirmForgotPasswordAsync(new ConfirmForgotPasswordRequest
            {
                ClientId = _clientId,
                Username = email,
                ConfirmationCode = code,
                Password = password
            });
        }
        catch (CodeMismatchException) { return Response(BadRequest, new { Message = "Password recovery could not be completed" }); }
        catch (ExpiredCodeException) { return Response(BadRequest, new { Message = "Password recovery could not be completed" }); }
        catch (InvalidPasswordException) { return Response(BadRequest, new { Message = "Password recovery could not be completed" }); }
        catch (InvalidParameterException) { return Response(BadRequest, new { Message = "Password recovery could not be completed" }); }
        catch (UserNotFoundException) { return Response(BadRequest, new { Message = "Password recovery could not be completed" }); }
        catch
        {
            return Response(ServiceUnavailable, new { Message = "Password recovery unavailable" });
        }

        return Response(OK, new { Message = "Password updated. Authenticate again to obtain a new token" });
    }
}
