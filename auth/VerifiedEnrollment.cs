#nullable enable

using Newtonsoft.Json.Linq;
using AccountModel = Janus.Model.Account;

namespace Janus;

// The Lambda Cognito trigger is the authority for confirmed ownership.
// Never accept a caller-supplied 'confirmed' Boolean from an HTTP request.
public static class VerifiedEnrollment
{
    public static AccountModel? FromCognitoEvent(JObject message)
    {
        if (message.Value<string>("triggerSource") != "PostConfirmation_ConfirmSignUp")
            return null;

        var attributes = message["request"]?["userAttributes"];
        var verified = attributes?["email_verified"]?.Value<string>();
        var subject = attributes?["sub"]?.Value<string>();
        var email = attributes?["email"]?.Value<string>();

        if (!string.Equals(verified, "true", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException("Confirmed signup event must have verified email, subject and email");

        return new AccountModel { Id = subject, Email = email };
    }
}
