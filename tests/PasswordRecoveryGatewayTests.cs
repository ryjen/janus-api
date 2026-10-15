#nullable enable

using Amazon;
using Amazon.Runtime;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Janus;
using Xunit;

namespace Janus.Tests;

public sealed class PasswordRecoveryGatewayTests
{
    private sealed class FakeRecoveryGateway : IPasswordRecoveryGateway
    {
        public int StartCalls { get; private set; }
        public int ConfirmCalls { get; private set; }
        public string? LastEmail { get; private set; }
        public string? LastCode { get; private set; }
        public string? LastPassword { get; private set; }
        public Exception? StartFailure { get; set; }
        public Exception? ConfirmFailure { get; set; }

        public Task StartAsync(string email, CancellationToken cancellationToken = default)
        {
            StartCalls++;
            LastEmail = email;
            return StartFailure is null ? Task.CompletedTask : Task.FromException(StartFailure);
        }

        public Task ConfirmAsync(
            string email, string code, string newPassword, CancellationToken cancellationToken = default)
        {
            ConfirmCalls++;
            LastEmail = email;
            LastCode = code;
            LastPassword = newPassword;
            return ConfirmFailure is null ? Task.CompletedTask : Task.FromException(ConfirmFailure);
        }
    }

    private static Auth Handler(FakeRecoveryGateway gateway)
    {
        var sdk = new AmazonCognitoIdentityProviderClient(
            new AnonymousAWSCredentials(), RegionEndpoint.USEast1);
        return new Auth(sdk, "test-pool", "test-client",
            enrollmentGateway: null, passwordRecoveryGateway: gateway);
    }

    [Fact]
    public async Task RequestRecoveryNormalizesEmailAndDoesNotRevealExistence()
    {
        var gateway = new FakeRecoveryGateway();
        var handler = Handler(gateway);
        var input = new Dictionary<string, object> { ["email"] = " owner@example.invalid " };

        var eligible = await handler.StartPasswordRecovery(input);
        gateway.StartFailure = new UserNotFoundException("hidden account");
        var unknown = await handler.StartPasswordRecovery(input);

        Assert.Equal(202, eligible.StatusCode);
        Assert.Equal(eligible.Body, unknown.Body);
        Assert.Equal(2, gateway.StartCalls);
        Assert.Equal("owner@example.invalid", gateway.LastEmail);
        Assert.DoesNotContain("hidden account", unknown.Body);
    }

    [Fact]
    public async Task MissingEmailIsBadRequestWithoutCallingGateway()
    {
        var gateway = new FakeRecoveryGateway();
        var response = await Handler(gateway).StartPasswordRecovery(new());
        Assert.Equal(400, response.StatusCode);
        Assert.Equal(0, gateway.StartCalls);
    }

    [Fact]
    public async Task UnavailableRecoveryIs503WithoutProviderDetails()
    {
        var gateway = new FakeRecoveryGateway
        {
            StartFailure = new InvalidOperationException("private-upstream-error")
        };
        var response = await Handler(gateway).StartPasswordRecovery(new()
        {
            ["email"] = "owner@example.invalid"
        });
        Assert.Equal(503, response.StatusCode);
        Assert.DoesNotContain("private-upstream-error", response.Body);
    }

    [Fact]
    public async Task ConfirmationPreservesNewPasswordVerbatim()
    {
        var gateway = new FakeRecoveryGateway();
        var response = await Handler(gateway).ConfirmPasswordRecovery(new()
        {
            ["email"] = " owner@example.invalid ",
            ["code"] = " 123456 ",
            ["password"] = " \tnew password\n "
        });

        Assert.Equal(200, response.StatusCode);
        Assert.Equal(1, gateway.ConfirmCalls);
        Assert.Equal("owner@example.invalid", gateway.LastEmail);
        Assert.Equal("123456", gateway.LastCode);
        Assert.Equal(" \tnew password\n ", gateway.LastPassword);
        Assert.DoesNotContain("new password", response.Body);
    }

    [Fact]
    public async Task NonStringConfirmationPasswordDoesNotCallGateway()
    {
        var gateway = new FakeRecoveryGateway();
        var response = await Handler(gateway).ConfirmPasswordRecovery(new()
        {
            ["email"] = "owner@example.invalid",
            ["code"] = "123456",
            ["password"] = 12345
        });
        Assert.Equal(400, response.StatusCode);
        Assert.Equal(0, gateway.ConfirmCalls);
    }

    [Fact]
    public async Task IncorrectConfirmationCodeIsGeneric400()
    {
        var gateway = new FakeRecoveryGateway
        {
            ConfirmFailure = new CodeMismatchException("submitted-secret-code")
        };
        var response = await Handler(gateway).ConfirmPasswordRecovery(new()
        {
            ["email"] = "owner@example.invalid",
            ["code"] = "123456",
            ["password"] = "valid-test-password"
        });
        Assert.Equal(400, response.StatusCode);
        Assert.DoesNotContain("submitted-secret-code", response.Body);
    }

    [Fact]
    public async Task ConfirmationProviderFailureIsGeneric503()
    {
        var gateway = new FakeRecoveryGateway
        {
            ConfirmFailure = new InvalidOperationException("private-upstream-error")
        };
        var response = await Handler(gateway).ConfirmPasswordRecovery(new()
        {
            ["email"] = "owner@example.invalid",
            ["code"] = "123456",
            ["password"] = "valid-test-password"
        });
        Assert.Equal(503, response.StatusCode);
        Assert.DoesNotContain("private-upstream-error", response.Body);
    }
}
