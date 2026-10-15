#nullable enable

using Amazon.CognitoIdentityProvider.Model;

namespace Janus;

// Known credential/challenge denials versus all other SDK/network/program faults.
// Fail closed: unfamiliar provider exceptions are not evidence of a bad password.
public enum CognitoAuthenticationFailure
{
    Rejected,
    ProviderUnavailable
}

public static class CognitoAuthenticationErrorPolicy
{
    public static CognitoAuthenticationFailure Classify(Exception error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return error switch
        {
            NotAuthorizedException or UserNotFoundException or UserNotConfirmedException or
            PasswordResetRequiredException or InvalidPasswordException or
            CodeMismatchException or ExpiredCodeException => CognitoAuthenticationFailure.Rejected,
            _ => CognitoAuthenticationFailure.ProviderUnavailable
        };
    }
}
