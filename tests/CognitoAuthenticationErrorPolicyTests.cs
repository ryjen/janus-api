#nullable enable

using Amazon;
using Amazon.Runtime;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Janus;
using Xunit;

namespace Janus.Tests;

public sealed class CognitoAuthenticationErrorPolicyTests
{
    private sealed class FakeCognito : AmazonCognitoIdentityProviderClient
    {
        public Exception? AuthenticationError { get; set; }
        public Exception? ChallengeError { get; set; }
        public int AuthCalls { get; private set; }
        public int ChallengeCalls { get; private set; }

        public FakeCognito() : base(new AnonymousAWSCredentials(), RegionEndpoint.USEast1) { }

        public override Task<AdminInitiateAuthResponse> AdminInitiateAuthAsync(
            AdminInitiateAuthRequest request, CancellationToken cancellationToken = default)
        {
            AuthCalls++;
            return AuthenticationError is null
                ? Task.FromResult(new AdminInitiateAuthResponse
                {
                    AuthenticationResult = new AuthenticationResultType { IdToken = "synthetic-token" }
                })
                : Task.FromException<AdminInitiateAuthResponse>(AuthenticationError);
        }

        public override Task<AdminRespondToAuthChallengeResponse> AdminRespondToAuthChallengeAsync(
            AdminRespondToAuthChallengeRequest request, CancellationToken cancellationToken = default)
        {
            ChallengeCalls++;
            return ChallengeError is null
                ? Task.FromResult(new AdminRespondToAuthChallengeResponse
                {
                    AuthenticationResult = new AuthenticationResultType { IdToken = "synthetic-token" }
                })
                : Task.FromException<AdminRespondToAuthChallengeResponse>(ChallengeError);
        }
    }

    public static IEnumerable<object[]> Classifications()
    {
        yield return new object[] { new NotAuthorizedException("sensitive invalid credentials"), CognitoAuthenticationFailure.Rejected };
        yield return new object[] { new UserNotFoundException("sensitive missing user"), CognitoAuthenticationFailure.Rejected };
        yield return new object[] { new UserNotConfirmedException("sensitive unconfirmed user"), CognitoAuthenticationFailure.Rejected };
        yield return new object[] { new PasswordResetRequiredException("sensitive reset state"), CognitoAuthenticationFailure.Rejected };
        yield return new object[] { new CodeMismatchException("sensitive incorrect code"), CognitoAuthenticationFailure.Rejected };
        yield return new object[] { new ExpiredCodeException("sensitive expired code"), CognitoAuthenticationFailure.Rejected };
        yield return new object[] { new InvalidPasswordException("sensitive invalid password"), CognitoAuthenticationFailure.Rejected };
        yield return new object[] { new InternalErrorException("sensitive provider failure"), CognitoAuthenticationFailure.ProviderUnavailable };
        yield return new object[] { new InvalidParameterException("sensitive unexpected provider input"), CognitoAuthenticationFailure.ProviderUnavailable };
        yield return new object[] { new InvalidOperationException("sensitive unknown fault"), CognitoAuthenticationFailure.ProviderUnavailable };
    }

    [Theory]
    [MemberData(nameof(Classifications))]
    public void ClassifiesKnownRejectionsButDefaultsToUnavailable(Exception error, CognitoAuthenticationFailure expected)
    {
        Assert.Equal(expected, CognitoAuthenticationErrorPolicy.Classify(error));
    }

    [Fact]
    public void NullExceptionIsNotSilentlyAccepted() =>
        Assert.Throws<ArgumentNullException>(() => CognitoAuthenticationErrorPolicy.Classify(null!));

    private static Dictionary<string, object> LoginFields() => new()
    {
        ["email"] = "user@example.invalid",
        ["password"] = "sample secret password"
    };

    private static Dictionary<string, object> ChallengeFields() => new()
    {
        ["email"] = "user@example.invalid",
        ["password"] = "sample secret password",
        ["session"] = "sample challenge session"
    };

    [Fact]
    public async Task InvalidCredentialsRemain401WithoutLeakingSensitiveDetails()
    {
        using var fake = new FakeCognito { AuthenticationError =
            new NotAuthorizedException("sensitive sample secret password") };
        var response = await new Auth(fake, "test-pool", "test-client").Authenticate(LoginFields());
        Assert.Equal(401, response.StatusCode);
        Assert.Equal(1, fake.AuthCalls);
        Assert.DoesNotContain("sensitive", response.Body);
        Assert.DoesNotContain("sample secret password", response.Body);
    }

    [Fact]
    public async Task CognitoLoginOutageReturns503WithoutLeakingSensitiveDetails()
    {
        using var fake = new FakeCognito { AuthenticationError =
            new InternalErrorException("sensitive unexpected provider error") };
        var response = await new Auth(fake, "test-pool", "test-client").Authenticate(LoginFields());
        Assert.Equal(503, response.StatusCode);
        Assert.Equal(1, fake.AuthCalls);
        Assert.DoesNotContain("sensitive", response.Body);
        Assert.DoesNotContain("sample secret password", response.Body);
    }

    [Fact]
    public async Task RejectedPasswordChallengeRemains401()
    {
        using var fake = new FakeCognito { ChallengeError =
            new NotAuthorizedException("sensitive invalid session") };
        var response = await new Auth(fake, "test-pool", "test-client").NewPassword(ChallengeFields());
        Assert.Equal(401, response.StatusCode);
        Assert.Equal(1, fake.ChallengeCalls);
        Assert.DoesNotContain("sensitive", response.Body);
    }

    [Fact]
    public async Task PasswordChallengeOutageReturns503()
    {
        using var fake = new FakeCognito { ChallengeError =
            new InternalErrorException("sensitive upstream error") };
        var response = await new Auth(fake, "test-pool", "test-client").NewPassword(ChallengeFields());
        Assert.Equal(503, response.StatusCode);
        Assert.Equal(1, fake.ChallengeCalls);
        Assert.DoesNotContain("sensitive", response.Body);
        Assert.DoesNotContain("sample secret password", response.Body);
    }
}
