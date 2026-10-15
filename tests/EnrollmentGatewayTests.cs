#nullable enable

using Amazon;
using Amazon.Runtime;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Janus;
using Xunit;

namespace Janus.Tests;

public sealed class EnrollmentGatewayTests
{
    private sealed class FakeEnrollmentGateway : IEnrollmentGateway
    {
        public int SignUpCalls { get; private set; }
        public int ConfirmationCalls { get; private set; }
        public string? LastEmail { get; private set; }
        public string? LastPassword { get; private set; }
        public string? LastCode { get; private set; }
        public Exception? SignUpFailure { get; set; }
        public Exception? ConfirmationFailure { get; set; }

        public Task RequestSignUpAsync(
            string email, string password, CancellationToken cancellationToken = default)
        {
            SignUpCalls++;
            LastEmail = email;
            LastPassword = password;
            return SignUpFailure is null ? Task.CompletedTask : Task.FromException(SignUpFailure);
        }

        public Task ConfirmEmailAsync(
            string email, string confirmationCode, CancellationToken cancellationToken = default)
        {
            ConfirmationCalls++;
            LastEmail = email;
            LastCode = confirmationCode;
            return ConfirmationFailure is null ? Task.CompletedTask : Task.FromException(ConfirmationFailure);
        }
    }

    private static Auth Handler(FakeEnrollmentGateway gateway)
    {
        // The AWS client exists only to satisfy the legacy constructor; these
        // tests route enrollment exclusively through the injected fake port.
        var sdk = new AmazonCognitoIdentityProviderClient(
            new AnonymousAWSCredentials(), RegionEndpoint.USEast1);
        return new Auth(sdk, "test-pool", "test-client", gateway);
    }

    [Fact]
    public async Task SignupPassesNormalizedEmailAndExactPasswordThroughPort()
    {
        var gateway = new FakeEnrollmentGateway();
        var response = await Handler(gateway).SignUp(new()
        {
            ["email"] = " owner@example.invalid ",
            ["password"] = " significant spaces "
        });
        Assert.Equal(200, response.StatusCode);
        Assert.Equal(1, gateway.SignUpCalls);
        Assert.Equal("owner@example.invalid", gateway.LastEmail);
        Assert.Equal(" significant spaces ", gateway.LastPassword);
    }

    [Fact]
    public async Task InvalidSignupPayloadDoesNotCallPort()
    {
        var gateway = new FakeEnrollmentGateway();
        var response = await Handler(gateway).SignUp(new()
        {
            ["email"] = new { value = "owner@example.invalid" },
            ["password"] = "sample-password"
        });
        Assert.Equal(400, response.StatusCode);
        Assert.Equal(0, gateway.SignUpCalls);
    }

    [Fact]
    public async Task DuplicateSignupKeepsSameAcknowledgement()
    {
        var gateway = new FakeEnrollmentGateway();
        var handler = Handler(gateway);
        var input = new Dictionary<string, object>
        {
            ["email"] = "owner@example.invalid",
            ["password"] = "sample-password"
        };
        var normal = await handler.SignUp(input);
        gateway.SignUpFailure = new UsernameExistsException("existing-user-secret");
        var duplicate = await handler.SignUp(input);
        Assert.Equal(normal.StatusCode, duplicate.StatusCode);
        Assert.Equal(normal.Body, duplicate.Body);
        Assert.DoesNotContain("existing-user-secret", duplicate.Body);
    }

    [Fact]
    public async Task ProviderOutageDoesNotLookLikeMalformedSignup()
    {
        var gateway = new FakeEnrollmentGateway
        {
            SignUpFailure = new InvalidOperationException("upstream-private-details")
        };
        var response = await Handler(gateway).SignUp(new()
        {
            ["email"] = "owner@example.invalid",
            ["password"] = "sample-password"
        });
        Assert.Equal(503, response.StatusCode);
        Assert.DoesNotContain("upstream-private-details", response.Body);
    }

    [Fact]
    public async Task ConfirmationUsesStringCodeAndDoesNotProvisionAccount()
    {
        var gateway = new FakeEnrollmentGateway();
        var response = await Handler(gateway).ConfirmSignUp(new()
        {
            ["email"] = " owner@example.invalid ",
            ["code"] = " 123456 "
        });
        Assert.Equal(200, response.StatusCode);
        Assert.Equal(1, gateway.ConfirmationCalls);
        Assert.Equal(0, gateway.SignUpCalls);
        Assert.Equal("owner@example.invalid", gateway.LastEmail);
        Assert.Equal("123456", gateway.LastCode);
    }

    [Fact]
    public async Task ConfirmationRejectsBadCodeWithoutProviderDetail()
    {
        var gateway = new FakeEnrollmentGateway
        {
            ConfirmationFailure = new CodeMismatchException("private-code")
        };
        var response = await Handler(gateway).ConfirmSignUp(new()
        {
            ["email"] = "owner@example.invalid",
            ["code"] = "123456"
        });
        Assert.Equal(400, response.StatusCode);
        Assert.DoesNotContain("private-code", response.Body);
    }

    [Fact]
    public async Task ConfirmationProviderOutageReturns503()
    {
        var gateway = new FakeEnrollmentGateway
        {
            ConfirmationFailure = new InvalidOperationException("upstream-private-details")
        };
        var response = await Handler(gateway).ConfirmSignUp(new()
        {
            ["email"] = "owner@example.invalid",
            ["code"] = "123456"
        });
        Assert.Equal(503, response.StatusCode);
        Assert.DoesNotContain("upstream-private-details", response.Body);
    }
}
