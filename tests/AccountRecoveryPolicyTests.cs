using Janus;
using Xunit;

namespace Janus.Tests;

public sealed class AccountRecoveryPolicyTests
{
    private static Dictionary<string, string> Claims(string verified = "true") => new()
    {
        ["sub"] = "cognito-subject",
        ["email"] = "confirmed@example.invalid",
        ["email_verified"] = verified
    };

    [Fact]
    public void AcceptsVerifiedClaimsAndOnlyUsesTheirIdentity()
    {
        var a = AccountRecoveryPolicy.VerifiedAccount(Claims());
        Assert.NotNull(a);
        Assert.Equal("cognito-subject", a!.Id);
        Assert.Equal("confirmed@example.invalid", a.Email);
    }

    [Theory]
    [InlineData("false")]
    [InlineData("")]
    [InlineData("1")]
    public void DeniesUnverifiedIdentity(string verified)
    {
        Assert.Null(AccountRecoveryPolicy.VerifiedAccount(Claims(verified)));
    }

    [Fact]
    public void DeniesMissingSubject()
    {
        var claims = Claims();
        claims.Remove("sub");
        Assert.Null(AccountRecoveryPolicy.VerifiedAccount(claims));
    }

    [Fact]
    public void DeniesMissingEmail()
    {
        var claims = Claims();
        claims.Remove("email");
        Assert.Null(AccountRecoveryPolicy.VerifiedAccount(claims));
    }

    [Fact]
    public void DeniesMissingClaims() =>
        Assert.Null(AccountRecoveryPolicy.VerifiedAccount(null));
}
