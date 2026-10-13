using Amazon.Lambda.APIGatewayEvents;
using Amazon.CognitoIdentityProvider.Model;
using static System.Net.HttpStatusCode;

namespace Janus;

using static Shared;
using RequestParams = Dictionary<string, object>;

public partial class Auth
{
    // Cognito self-enrollment sends a confirmation code to the claimed email.
    // Do not provision an account based solely on an unverified signup request.
    public async Task<APIGatewayProxyResponse> SignUp(RequestParams request)
    {
        try
        {
            var email = PasswordRecoveryInput.Read(request, "email");
            var password = request.TryGetValue("password", out var rawPassword) && rawPassword is string suppliedPassword &&
                !string.IsNullOrWhiteSpace(suppliedPassword) ? suppliedPassword : null;
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return Response(BadRequest, new { Message = "Email and password required" });

            await _cognitoClient.SignUpAsync(new SignUpRequest
            {
                ClientId = _clientId,
                Username = email,
                Password = password,
                UserAttributes = new List<AttributeType> {
                    new AttributeType { Name = "email", Value = email }
                }
            });
            return SignupResponsePolicy.Acknowledged();
        }
        catch (UsernameExistsException)
        {
            // A duplicate address must look the same as an eligible signup.
            return SignupResponsePolicy.Acknowledged();
        }
        catch
        {
            return Response(BadRequest, new { Message = "Registration could not be completed" });
        }
    }

    public async Task<APIGatewayProxyResponse> ConfirmSignUp(RequestParams request)
    {
        try
        {
            var email = request["email"]?.ToString();
            var code = PasswordRecoveryInput.Read(request, "code");
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
                return Response(BadRequest, new { Message = "Email and confirmation code required" });

            await _cognitoClient.ConfirmSignUpAsync(new ConfirmSignUpRequest
            {
                ClientId = _clientId,
                Username = email,
                ConfirmationCode = code
            });
            // Only the Cognito PostConfirmation trigger can enqueue the account.
            return Response(OK, new { Confirmed = true });
        }
        catch
        {
            return Response(BadRequest, new { Message = "Confirmation could not be completed" });
        }
    }
}
