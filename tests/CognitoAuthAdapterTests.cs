#nullable enable

using Amazon;
using Amazon.Runtime;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Janus;
using Xunit;

namespace Janus.Tests;

public sealed class CognitoAuthAdapterTests
{
    private sealed class FakeCognito : AmazonCognitoIdentityProviderClient
    {
        public SignUpRequest? SignedUp { get; private set; }
        public ForgotPasswordRequest? PasswordRecovery { get; private set; }
        public bool DuplicateSignup { get; set; }
        public bool UnknownUser { get; set; }
        public bool ServiceFailure { get; set; }

        // Explicit fake credentials and region ensure no real AWS call, instance
        // profile lookup, or IAM connection is needed in these tests.
        public FakeCognito() : base(new AnonymousAWSCredentials(), RegionEndpoint.USEast1) { }

        public override Task<SignUpResponse> SignUpAsync(SignUpRequest request,
            CancellationToken cancellationToken = default)
        {
            SignedUp = request;
            if (DuplicateSignup)
                return Task.FromException<SignUpResponse>(new UsernameExistsException("User exists"));
            return Task.FromResult(new SignUpResponse { UserConfirmed = false });
        }

        public override Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request,
            CancellationToken cancellationToken = default)
        {
            PasswordRecovery = request;
            if (UnknownUser)
                return Task.FromException<ForgotPasswordResponse>(new UserNotFoundException("No user"));
            if (ServiceFailure)
                return Task.FromException<ForgotPasswordResponse>(new InvalidOperationException("Unavailable"));
            return Task.FromResult(new ForgotPasswordResponse());
        }
    }

    private static Dictionary<string, object> SignUpInput() => new()
    {
        ["email"] = "owner@example.invalid",
        ["password"] = "testing-not-a-real-credential"
    };

    [Fact]
    public async Task RegistrationUsesConfiguredClientAndStandardCognitoSignup()
    {
        var fake = new FakeCognito();
        var result = await new Auth(fake, "test-pool", "test-client").SignUp(SignUpInput());
        Assert.Equal(200, result.StatusCode);
        Assert.Equal("test-client", fake.SignedUp!.ClientId);
        Assert.Equal("owner@example.invalid", fake.SignedUp.Username);
        Assert.Single(fake.SignedUp.UserAttributes);
        Assert.Equal("email", fake.SignedUp.UserAttributes[0].Name);
    }

    [Fact]
    public async Task DuplicateSignupDoesNotRevealUserExistence()
    {
        var fake = new FakeCognito();
        var auth = new Auth(fake, "test-pool", "test-client");
        var ordinary = await auth.SignUp(SignUpInput());
        fake.DuplicateSignup = true;
        var duplicate = await auth.SignUp(SignUpInput());
        Assert.Equal(ordinary.StatusCode, duplicate.StatusCode);
        Assert.Equal(ordinary.Body, duplicate.Body);
    }

    [Fact]
    public async Task UnknownUserRecoveryUsesTheSameResponse()
    {
        var fake = new FakeCognito();
        var auth = new Auth(fake, "test-pool", "test-client");
        var request = new Dictionary<string, object> { ["email"] = "owner@example.invalid" };

        var known = await auth.StartPasswordRecovery(request);
        fake.UnknownUser = true;
        var unknown = await auth.StartPasswordRecovery(request);

        Assert.Equal(202, known.StatusCode);
        Assert.Equal(known.StatusCode, unknown.StatusCode);
        Assert.Equal(known.Body, unknown.Body);
        Assert.Equal("test-client", fake.PasswordRecovery!.ClientId);
    }

    [Fact]
    public async Task RecoveryServiceFailureIsNotMarkedSuccessful()
    {
        var fake = new FakeCognito { ServiceFailure = true };
        var result = await new Auth(fake, "test-pool", "test-client").StartPasswordRecovery(
            new Dictionary<string, object> { ["email"] = "owner@example.invalid" });
        Assert.Equal(503, result.StatusCode);
        Assert.DoesNotContain("Unavailable", result.Body);
    }

    [Fact]
    public void AuthRejectsAbsentIdentityConfiguration()
    {
        var fake = new FakeCognito();
        Assert.Throws<ArgumentException>(() => new Auth(fake, "", "test-client"));
        Assert.Throws<ArgumentException>(() => new Auth(fake, "test-pool", null));
    }
}
