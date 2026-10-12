using Janus;
using Xunit;

namespace Janus.Tests;

public sealed class PasswordRecoveryInputTests
{
    [Fact]
    public void RejectsMissingAndNullParameters()
    {
        Assert.Null(PasswordRecoveryInput.Read(null, "email"));
        Assert.Null(PasswordRecoveryInput.Read(new Dictionary<string, object>(), "email"));
        Assert.Null(PasswordRecoveryInput.Read(new Dictionary<string, object> { ["email"] = null! }, "email"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void RejectsEmptyEmail(string email)
        => Assert.Null(PasswordRecoveryInput.Read(new Dictionary<string, object> { ["email"] = email }, "email"));

    [Fact]
    public void RejectsStructuredValuesInsteadOfStringCoercion()
        => Assert.Null(PasswordRecoveryInput.Read(new Dictionary<string, object> { ["email"] = new { account = "a" } }, "email"));

    [Fact]
    public void ReturnsTrimmedString()
        => Assert.Equal("member@example.invalid",
            PasswordRecoveryInput.Read(new Dictionary<string, object> { ["email"] = " member@example.invalid " }, "email"));
}
