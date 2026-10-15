#nullable enable

using Amazon;
using Amazon.Runtime;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Lambda.APIGatewayEvents;
using Janus;
using Newtonsoft.Json;
using Xunit;

namespace Janus.Tests;

public sealed class PasswordRecoveryAdapterTests
{
    private sealed class FakeCognito : AmazonCognitoIdentityProviderClient
    {
        public ConfirmForgotPasswordRequest? Confirmed { get; private set; }
        public int Calls { get; private set; }
        public bool InvalidCode { get; set; }

        public FakeCognito() : base(new AnonymousAWSCredentials(), RegionEndpoint.USEast1) { }

        public override Task<ConfirmForgotPasswordResponse> ConfirmForgotPasswordAsync(
            ConfirmForgotPasswordRequest request, CancellationToken cancellationToken = default)
        {
            Calls++;
            Confirmed = request;
            if (InvalidCode)
                return Task.FromException<ConfirmForgotPasswordResponse>(
                    new CodeMismatchException("Code failed with sensitive upstream details"));
            return Task.FromResult(new ConfirmForgotPasswordResponse());
        }
    }

    private static Dictionary<string, object> ValidRequest(object password) => new()
    {
        ["email"] = " person@example.invalid ",
        ["code"] = " 123456 ",
        ["password"] = password
    };

    [Fact]
    public async Task ConfirmRecoveryPreservesPasswordWhitespaceVerbatim()
    {
        using var cognito = new FakeCognito();
        var response = await new Auth(cognito, "pool", "client").ConfirmPasswordRecovery(
            ValidRequest("  leading and trailing  "));

        Assert.Equal(200, response.StatusCode);
        Assert.Equal(1, cognito.Calls);
        Assert.Equal("  leading and trailing  ", cognito.Confirmed!.Password);
        Assert.Equal("person@example.invalid", cognito.Confirmed.Username);
        Assert.Equal("123456", cognito.Confirmed.ConfirmationCode);
        Assert.Equal("client", cognito.Confirmed.ClientId);
        Assert.DoesNotContain("leading and trailing", response.Body);
    }

    [Fact]
    public async Task HttpAdapterDoesNotNormalizeRecoveryPassword()
    {
        using var cognito = new FakeCognito();
        var body = JsonConvert.SerializeObject(ValidRequest(" \tpassphrase \n "));
        var response = await new Auth(cognito, "pool", "client").Handler(
            new APIGatewayProxyRequest { Path = "/password/confirm", Body = body }, null!);

        Assert.Equal(200, response.StatusCode);
        Assert.Equal(" \tpassphrase \n ", cognito.Confirmed!.Password);
        Assert.Equal(1, cognito.Calls);
    }

    [Fact]
    public async Task StructuredAndNumericRecoveryPasswordsFailBeforeCognito()
    {
        using var cognito = new FakeCognito();
        var auth = new Auth(cognito, "pool", "client");
        foreach (var invalid in new object[] { new { value = "secret" }, 12345, new[] { "p", "w" } })
        {
            var response = await auth.ConfirmPasswordRecovery(ValidRequest(invalid));
            Assert.Equal(400, response.StatusCode);
            Assert.DoesNotContain("secret", response.Body);
        }

        Assert.Equal(0, cognito.Calls);
        Assert.Null(cognito.Confirmed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BlankRecoveryPasswordsFailBeforeCognito(string password)
    {
        using var cognito = new FakeCognito();
        var response = await new Auth(cognito, "pool", "client").ConfirmPasswordRecovery(
            ValidRequest(password));

        Assert.Equal(400, response.StatusCode);
        Assert.Equal(0, cognito.Calls);
    }

    [Fact]
    public async Task CognitoCodeFailureIsRedacted()
    {
        using var cognito = new FakeCognito { InvalidCode = true };
        var response = await new Auth(cognito, "pool", "client").ConfirmPasswordRecovery(
            ValidRequest("  sample-password  "));

        Assert.Equal(400, response.StatusCode);
        Assert.Equal(1, cognito.Calls);
        Assert.DoesNotContain("sensitive upstream details", response.Body);
        Assert.DoesNotContain("sample-password", response.Body);
    }
}
