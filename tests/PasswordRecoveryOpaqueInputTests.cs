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

public sealed class PasswordRecoveryOpaqueInputTests
{
    private sealed class FakeCognito : AmazonCognitoIdentityProviderClient
    {
        public ConfirmForgotPasswordRequest? Confirmation { get; private set; }
        public int Calls { get; private set; }

        public FakeCognito() : base(new AnonymousAWSCredentials(), RegionEndpoint.USEast1) { }

        public override Task<ConfirmForgotPasswordResponse> ConfirmForgotPasswordAsync(
            ConfirmForgotPasswordRequest request, CancellationToken cancellationToken = default)
        {
            Calls++;
            Confirmation = request;
            return Task.FromResult(new ConfirmForgotPasswordResponse());
        }
    }

    private static Dictionary<string, object> Fields() => new()
    {
        ["email"] = " owner@example.invalid ",
        ["code"] = " 123456 ",
        ["password"] = " leading and trailing "
    };

    [Fact]
    public async Task RecoveryConfirmationPreservesPasswordAndNormalizesEmailAndCode()
    {
        using var fake = new FakeCognito();
        var response = await new Auth(fake, "pool", "client").ConfirmPasswordRecovery(Fields());

        Assert.Equal(200, response.StatusCode);
        Assert.Equal(1, fake.Calls);
        Assert.Equal(" leading and trailing ", fake.Confirmation!.Password);
        Assert.Equal("owner@example.invalid", fake.Confirmation.Username);
        Assert.Equal("123456", fake.Confirmation.ConfirmationCode);
        Assert.Equal("client", fake.Confirmation.ClientId);
        Assert.DoesNotContain("leading and trailing", response.Body);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(123)]
    [InlineData(true)]
    public async Task RecoveryConfirmationRejectsMissingOrNonStringPasswords(object? value)
    {
        using var fake = new FakeCognito();
        var fields = Fields();
        fields["password"] = value!;
        var response = await new Auth(fake, "pool", "client")
            .ConfirmPasswordRecovery(fields);

        Assert.Equal(400, response.StatusCode);
        Assert.Equal(0, fake.Calls);
    }

    [Fact]
    public async Task HttpAdapterPreservesPasswordWithoutEcho()
    {
        using var fake = new FakeCognito();
        var response = await new Auth(fake, "pool", "client").Handler(
            new APIGatewayProxyRequest
            {
                Path = "/password/confirm",
                Body = JsonConvert.SerializeObject(Fields())
            }, null!);
        Assert.Equal(200, response.StatusCode);
        Assert.Equal(" leading and trailing ", fake.Confirmation!.Password);
        Assert.DoesNotContain("leading and trailing", response.Body);
    }

    [Fact]
    public async Task RejectsStructuredPasswordBeforeCallingCognito()
    {
        using var fake = new FakeCognito();
        var fields = Fields();
        fields["password"] = new { nested = "not-a-password" };
        var response = await new Auth(fake, "pool", "client").Handler(
            new APIGatewayProxyRequest
            {
                Path = "/password/confirm",
                Body = JsonConvert.SerializeObject(fields)
            }, null!);
        Assert.Equal(400, response.StatusCode);
        Assert.Equal(0, fake.Calls);
        Assert.DoesNotContain("not-a-password", response.Body);
    }
}
