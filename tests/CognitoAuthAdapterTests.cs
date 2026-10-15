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
        public ConfirmSignUpRequest? Confirmed { get; private set; }

        public bool DuplicateSignup { get; set; }
        public bool UnknownUser { get; set; }
        public bool ServiceFailure { get; set; }
        public Exception? SignupError { get; set; }
        public Exception? ConfirmError { get; set; }

        // Explicit fake credentials and region ensure no real AWS call, instance
        // profile lookup, or IAM connection is needed in these tests.
        public FakeCognito() : base(new AnonymousAWSCredentials(), RegionEndpoint.USEast1) { }

        public override Task<SignUpResponse> SignUpAsync(SignUpRequest request,
            CancellationToken cancellationToken = default)
        {
            SignedUp = request;
            if (SignupError is not null)
                return Task.FromException<SignUpResponse>(SignupError);
            if (DuplicateSignup)
                return Task.FromException<SignUpResponse>(new UsernameExistsException("User exists"));
            return Task.FromResult(new SignUpResponse { UserConfirmed = false });
        }

        public override Task<ConfirmSignUpResponse> ConfirmSignUpAsync(ConfirmSignUpRequest request,
            CancellationToken cancellationToken = default)
        {
            Confirmed = request;
            if (ConfirmError is not null)
                return Task.FromException<ConfirmSignUpResponse>(ConfirmError);
            return Task.FromResult(new ConfirmSignUpResponse());
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

    [Fact]
    public async Task SignupRejectsNonStringCredentialsWithoutCognitoCall()
    {
        var fake = new FakeCognito();
        var input = new Dictionary<string, object>
        {
            ["email"] = 123,
            ["password"] = "safe-test-password"
        };
        var response = await new Auth(fake, "test-pool", "test-client").SignUp(input);
        Assert.Equal(400, response.StatusCode);
        Assert.Null(fake.SignedUp);
    }

    [Fact]
    public async Task SignupPreservesSignificantPasswordWhitespace()
    {
        var fake = new FakeCognito();
        var input = new Dictionary<string, object>
        {
            ["email"] = " owner@example.invalid ",
            ["password"] = " leading-and-trailing "
        };
        var response = await new Auth(fake, "test-pool", "test-client").SignUp(input);
        Assert.Equal(200, response.StatusCode);
        Assert.Equal(" leading-and-trailing ", fake.SignedUp!.Password);
        Assert.Equal("owner@example.invalid", fake.SignedUp.Username);
    }

    [Fact]
    public async Task ConfirmationRejectsNonStringCodeWithoutCognitoCall()
    {
        var fake = new FakeCognito();
        var input = new Dictionary<string, object>
        {
            ["email"] = "owner@example.invalid",
            ["code"] = 123456
        };
        var response = await new Auth(fake, "test-pool", "test-client").ConfirmSignUp(input);
        Assert.Equal(400, response.StatusCode);
        Assert.Null(fake.Confirmed);
    }

    [Fact]
    public async Task SignupRejectsNonStringPasswordBeforeCognito()
    {
        var fake = new FakeCognito();
        var response = await new Auth(fake, "test-pool", "test-client").SignUp(
            new Dictionary<string, object>
            {
                ["email"] = "owner@example.invalid",
                ["password"] = 12345
            });
        Assert.Equal(400, response.StatusCode);
        Assert.Null(fake.SignedUp);
    }

    [Fact]
    public async Task ConfirmationRejectsNonStringEmailBeforeCognito()
    {
        var fake = new FakeCognito();
        var response = await new Auth(fake, "test-pool", "test-client").ConfirmSignUp(
            new Dictionary<string, object>
            {
                ["email"] = new { Value = "owner@example.invalid" },
                ["code"] = "123456"
            });
        Assert.Equal(400, response.StatusCode);
        Assert.Null(fake.Confirmed);
    }

    [Fact]
    public async Task ConfirmationUsesTrimmedEmailAndStringCode()
    {
        var fake = new FakeCognito();
        var response = await new Auth(fake, "test-pool", "test-client").ConfirmSignUp(
            new Dictionary<string, object>
            {
                ["email"] = " owner@example.invalid ",
                ["code"] = " 123456 "
            });
        Assert.Equal(200, response.StatusCode);
        Assert.Equal("owner@example.invalid", fake.Confirmed!.Username);
        Assert.Equal("123456", fake.Confirmed.ConfirmationCode);
        Assert.Equal("test-client", fake.Confirmed.ClientId);
    }

    [Fact]
    public async Task CognitoInvalidSignupPasswordRemains400WithoutDetails()
    {
        using var fake = new FakeCognito
        {
            SignupError = new InvalidPasswordException("Password and user details are sensitive")
        };
        var response = await new Auth(fake, "test-pool", "test-client").SignUp(SignUpInput());
        Assert.Equal(400, response.StatusCode);
        Assert.NotNull(fake.SignedUp);
        Assert.DoesNotContain("sensitive", response.Body);
        Assert.DoesNotContain("testing-not-a-real-credential", response.Body);
    }

    [Fact]
    public async Task CognitoInvalidSignupParametersRemain400WithoutDetails()
    {
        using var fake = new FakeCognito
        {
            SignupError = new InvalidParameterException("Sensitive invalid parameter")
        };
        var response = await new Auth(fake, "test-pool", "test-client").SignUp(SignUpInput());
        Assert.Equal(400, response.StatusCode);
        Assert.DoesNotContain("Sensitive", response.Body);
    }

    [Fact]
    public async Task SignupProviderFailureReturns503InsteadOfBadRequest()
    {
        using var fake = new FakeCognito
        {
            SignupError = new InternalErrorException("Sensitive upstream fault")
        };
        var response = await new Auth(fake, "test-pool", "test-client").SignUp(SignUpInput());
        Assert.Equal(503, response.StatusCode);
        Assert.DoesNotContain("Sensitive", response.Body);
    }

    [Theory]
    [InlineData("wrong")]
    [InlineData("expired")]
    [InlineData("missing")]
    public async Task InvalidConfirmationCodeResponsesAreGeneric400(string kind)
    {
        using var fake = new FakeCognito
        {
            ConfirmError = kind switch
            {
                "wrong" => new CodeMismatchException("Wrong code with sensitive details"),
                "expired" => new ExpiredCodeException("Expired code with sensitive details"),
                _ => new UserNotFoundException("Unknown account with sensitive details")
            }
        };
        var response = await new Auth(fake, "test-pool", "test-client").ConfirmSignUp(
            new Dictionary<string, object> {
                ["email"] = "owner@example.invalid", ["code"] = "sample-code"
            });
        Assert.Equal(400, response.StatusCode);
        Assert.NotNull(fake.Confirmed);
        Assert.DoesNotContain("sensitive", response.Body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sample-code", response.Body);
    }

    [Fact]
    public async Task ConfirmationProviderFailureReturns503InsteadOfBadCode()
    {
        using var fake = new FakeCognito
        {
            ConfirmError = new InternalErrorException("Sensitive upstream fault")
        };
        var response = await new Auth(fake, "test-pool", "test-client").ConfirmSignUp(
            new Dictionary<string, object> {
                ["email"] = "owner@example.invalid", ["code"] = "sample-code"
            });
        Assert.Equal(503, response.StatusCode);
        Assert.DoesNotContain("Sensitive", response.Body);
        Assert.DoesNotContain("sample-code", response.Body);
    }

}
