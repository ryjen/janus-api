#nullable enable
namespace Janus;

// API Gateway validates Cognito's ID token before passing authorizer claims
// to Lambda. The subject and email must come exclusively from those claims.
public static class AccountRecoveryPolicy
{
    public static Model.Account? VerifiedAccount(IDictionary<string, string>? claims)
    {
        if (claims is null ||
            !claims.TryGetValue("sub", out var sub) || string.IsNullOrWhiteSpace(sub) ||
            !claims.TryGetValue("email", out var email) || string.IsNullOrWhiteSpace(email) ||
            !claims.TryGetValue("email_verified", out var verified) ||
            !string.Equals(verified, "true", StringComparison.OrdinalIgnoreCase))
            return null;

        return new Model.Account { Id = sub, Email = email };
    }
}
