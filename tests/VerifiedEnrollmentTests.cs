using Janus;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Janus.Tests;

public class VerifiedEnrollmentTests
{
    private static JObject Event(string trigger, string verified, string sub = "user-123", string email = "member@example.invalid")
        => JObject.Parse($$"""
        {
          "triggerSource": "{{trigger}}",
          "request": {"userAttributes": {
            "sub": "{{sub}}",
            "email": "{{email}}",
            "email_verified": "{{verified}}"
          }}
        }
        """);

    [Fact]
    public void ConfirmedAndEmailVerifiedSignupAllowsProvisioning()
    {
        var account = VerifiedEnrollment.FromCognitoEvent(Event("PostConfirmation_ConfirmSignUp", "true"));
        Assert.NotNull(account);
        Assert.Equal("user-123", account!.Id);
        Assert.Equal("member@example.invalid", account.Email);
    }

    [Theory]
    [InlineData("PostConfirmation_ConfirmForgotPassword")]
    [InlineData("PostConfirmation_ConfirmSignUp_Other")]
    public void IrrelevantTriggersNeverProvision(string trigger)
        => Assert.Null(VerifiedEnrollment.FromCognitoEvent(Event(trigger, "true")));

    [Theory]
    [InlineData("false")]
    [InlineData("")]
    public void UnverifiedSignupsFailClosed(string verified)
        => Assert.Throws<InvalidOperationException>(() =>
            VerifiedEnrollment.FromCognitoEvent(Event("PostConfirmation_ConfirmSignUp", verified)));

    [Fact]
    public void MissingSubjectFailsClosed()
        => Assert.Throws<InvalidOperationException>(() =>
            VerifiedEnrollment.FromCognitoEvent(Event("PostConfirmation_ConfirmSignUp", "true", "")));
}
