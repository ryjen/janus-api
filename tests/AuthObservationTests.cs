#nullable enable

using System.Reflection;
using Amazon;
using Amazon.Runtime;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Lambda.APIGatewayEvents;
using Janus;
using Newtonsoft.Json;
using Xunit;

namespace Janus.Tests;

public sealed class AuthObservationTests
{
    private const string TestEmail = "observer@example.invalid";
    private const string TestPassword = "sensitive-test-password";
    private const string TestSession = "sensitive-test-session";
    private const string TestCode = "sensitive-test-code";
    private const string TestToken = "sensitive-test-token";
    private const string TestPool = "test-pool";
    private const string TestClient = "test-client";

    private sealed class Enrollment : IEnrollmentGateway
    {
        public Exception? Failure { get; set; }
        public Task RequestSignUpAsync(
            string email, string password, CancellationToken cancellationToken = default) =>
            Failure is null ? Task.CompletedTask : Task.FromException(Failure);
        public Task ConfirmEmailAsync(
            string email, string code, CancellationToken cancellationToken = default) =>
            Failure is null ? Task.CompletedTask : Task.FromException(Failure);
    }

    private sealed class Recovery : IPasswordRecoveryGateway
    {
        public Exception? Failure { get; set; }
        public Task StartAsync(string email, CancellationToken cancellationToken = default) =>
            Failure is null ? Task.CompletedTask : Task.FromException(Failure);
        public Task ConfirmAsync(
            string email, string code, string password, CancellationToken cancellationToken = default) =>
            Failure is null ? Task.CompletedTask : Task.FromException(Failure);
    }

    private sealed class Authentication : IAuthenticationGateway
    {
        public Exception? Failure { get; set; }
        public LoginSessionResult LoginResult { get; set; } = new(TestToken, null, null);

        public Task<LoginSessionResult> AuthenticateAsync(
            string email, string password, CancellationToken cancellationToken = default) =>
            Failure is null ? Task.FromResult(LoginResult) :
                Task.FromException<LoginSessionResult>(Failure);

        public Task<string?> RespondToNewPasswordAsync(
            string email, string password, string session, CancellationToken cancellationToken = default) =>
            Failure is null ? Task.FromResult<string?>(TestToken) :
                Task.FromException<string?>(Failure);
    }

    private static Auth Create(
        AuthObservationCounter sink, Enrollment? enrollment = null, Recovery? recovery = null,
        Authentication? authentication = null)
    {
        // All gateway operations use injected fakes; this client never calls AWS.
        var sdk = new AmazonCognitoIdentityProviderClient(
            new AnonymousAWSCredentials(), RegionEndpoint.USEast1);
        return new Auth(sdk, TestPool, TestClient,
            enrollmentGateway: enrollment ?? new Enrollment(),
            passwordRecoveryGateway: recovery ?? new Recovery(),
            authenticationGateway: authentication ?? new Authentication(),
            observations: sink);
    }

    private static APIGatewayProxyRequest Request(string path, object fields) => new()
    {
        Path = path,
        Body = JsonConvert.SerializeObject(fields)
    };

    private static void AssertSingle(
        AuthObservationCounter sink, AuthRoute route, AuthObservationKind kind)
    {
        Assert.Equal(1, sink.TotalCount);
        Assert.Equal(1, sink.Count(route, kind));
        var stringified = new AuthObservation(route, kind).ToString();
        Assert.DoesNotContain(TestEmail, stringified);
        Assert.DoesNotContain(TestPassword, stringified);
        Assert.DoesNotContain(TestSession, stringified);
        Assert.DoesNotContain(TestToken, stringified);
        Assert.DoesNotContain(TestCode, stringified);
    }

    [Fact]
    public void LegacySixParameterConstructorKeepsBinarySignature()
    {
        var constructor = typeof(Auth).GetConstructor(new[]
        {
            typeof(IAmazonCognitoIdentityProvider),
            typeof(string),
            typeof(string),
            typeof(IEnrollmentGateway),
            typeof(IPasswordRecoveryGateway),
            typeof(IAuthenticationGateway)
        });
        Assert.NotNull(constructor);
    }

    [Fact]
    public void ObservationContractHasOnlyTwoEnumDimensions()
    {
        var properties = typeof(AuthObservation).GetProperties(
            BindingFlags.Instance | BindingFlags.Public);
        Assert.Equal(2, properties.Length);
        Assert.Contains(properties, x => x.Name == nameof(AuthObservation.Route) &&
            x.PropertyType == typeof(AuthRoute));
        Assert.Contains(properties, x => x.Name == nameof(AuthObservation.Kind) &&
            x.PropertyType == typeof(AuthObservationKind));
        Assert.All(properties, p => Assert.True(p.PropertyType.IsEnum));
    }

    [Theory]
    [InlineData(AuthRoutes.AuthenticatePath, AuthRoute.Authenticate)]
    [InlineData(AuthRoutes.NewPasswordChallengePath, AuthRoute.NewPasswordChallenge)]
    [InlineData(AuthRoutes.SignUpPath, AuthRoute.SignUp)]
    [InlineData(AuthRoutes.ConfirmSignUpPath, AuthRoute.ConfirmSignUp)]
    [InlineData(AuthRoutes.ForgotPasswordPath, AuthRoute.ForgotPassword)]
    [InlineData(AuthRoutes.ConfirmForgotPasswordPath, AuthRoute.ConfirmForgotPassword)]
    public async Task MalformedPayloadEmitsOnlyOneCategoricalEvent(
        string path, AuthRoute route)
    {
        var sink = new AuthObservationCounter();
        var response = await Create(sink).Handler(new APIGatewayProxyRequest
        {
            Path = path,
            Body = "{\"password\":\"" + TestPassword
        }, null!);
        Assert.Equal(400, response.StatusCode);
        AssertSingle(sink, route, AuthObservationKind.InvalidInput);
    }

    [Fact]
    public async Task UnknownUserPathNeverBecomesMetricLabel()
    {
        var sink = new AuthObservationCounter();
        var response = await Create(sink).Handler(new APIGatewayProxyRequest
        {
            Path = "/user-supplied-" + TestPassword,
            Body = "{invalid"
        }, null!);
        Assert.Equal(401, response.StatusCode);
        AssertSingle(sink, AuthRoute.Unknown, AuthObservationKind.DeniedOrChallenged);
    }

    [Fact]
    public async Task SuccessfulLoginEmitsOneSuccessWithoutToken()
    {
        var sink = new AuthObservationCounter();
        var response = await Create(sink).Handler(Request(AuthRoutes.AuthenticatePath, new
        {
            email = TestEmail,
            password = TestPassword
        }), null!);
        Assert.Equal(200, response.StatusCode);
        Assert.Contains(TestToken, response.Body);
        AssertSingle(sink, AuthRoute.Authenticate, AuthObservationKind.Succeeded);
    }

    [Fact]
    public async Task CognitoChallengeAndDenialShareNonIdentifyingObservation()
    {
        var sink = new AuthObservationCounter();
        var gateway = new Authentication
        {
            LoginResult = new(null, "NEW_PASSWORD_REQUIRED", TestSession)
        };
        var response = await Create(sink, authentication: gateway).Handler(
            Request(AuthRoutes.AuthenticatePath, new
            {
                email = TestEmail,
                password = TestPassword
            }), null!);
        Assert.Equal(401, response.StatusCode);
        Assert.Contains(TestSession, response.Body);
        AssertSingle(sink, AuthRoute.Authenticate, AuthObservationKind.DeniedOrChallenged);
    }

    [Fact]
    public async Task CredentialRejectionCannotExposeSdkExceptionText()
    {
        var sink = new AuthObservationCounter();
        var gateway = new Authentication
        {
            Failure = new NotAuthorizedException(TestPassword)
        };
        var response = await Create(sink, authentication: gateway).Handler(
            Request(AuthRoutes.AuthenticatePath, new
            {
                email = TestEmail, password = TestPassword
            }), null!);
        Assert.Equal(401, response.StatusCode);
        AssertSingle(sink, AuthRoute.Authenticate, AuthObservationKind.DeniedOrChallenged);
    }

    [Fact]
    public async Task ProviderFaultIsCategoricalUnavailable()
    {
        var sink = new AuthObservationCounter();
        var gateway = new Authentication
        {
            Failure = new InternalErrorException(TestPassword)
        };
        var response = await Create(sink, authentication: gateway).Handler(
            Request(AuthRoutes.AuthenticatePath, new
            {
                email = TestEmail, password = TestPassword
            }), null!);
        Assert.Equal(503, response.StatusCode);
        AssertSingle(sink, AuthRoute.Authenticate, AuthObservationKind.Unavailable);
    }

    [Fact]
    public async Task AcceptedRecoveryEmitsOnlyCoarseAcceptedEvent()
    {
        var sink = new AuthObservationCounter();
        var response = await Create(sink).Handler(
            Request(AuthRoutes.ForgotPasswordPath, new { email = TestEmail }), null!);
        Assert.Equal(202, response.StatusCode);
        AssertSingle(sink, AuthRoute.ForgotPassword, AuthObservationKind.Accepted);
    }

    [Fact]
    public async Task KnownAndUnknownRecoveryUsersHaveIdenticalObservations()
    {
        var knownSink = new AuthObservationCounter();
        var unknownSink = new AuthObservationCounter();
        var input = Request(AuthRoutes.ForgotPasswordPath, new { email = TestEmail });

        var knownResponse = await Create(knownSink).Handler(input, null!);
        var unknownResponse = await Create(unknownSink,
            recovery: new Recovery { Failure = new UserNotFoundException(TestEmail) })
            .Handler(input, null!);

        Assert.Equal(knownResponse.StatusCode, unknownResponse.StatusCode);
        Assert.Equal(knownResponse.Body, unknownResponse.Body);
        AssertSingle(knownSink, AuthRoute.ForgotPassword, AuthObservationKind.Accepted);
        AssertSingle(unknownSink, AuthRoute.ForgotPassword, AuthObservationKind.Accepted);
    }

    [Fact]
    public async Task DuplicateSignUpRetainsSameObservationAsNormalSignup()
    {
        var input = Request(AuthRoutes.SignUpPath, new
        {
            email = TestEmail, password = TestPassword
        });
        var normalSink = new AuthObservationCounter();
        var duplicateSink = new AuthObservationCounter();

        var ordinary = await Create(normalSink).Handler(input, null!);
        var duplicate = await Create(duplicateSink,
            enrollment: new Enrollment { Failure = new UsernameExistsException(TestEmail) })
            .Handler(input, null!);

        Assert.Equal(ordinary.StatusCode, duplicate.StatusCode);
        Assert.Equal(ordinary.Body, duplicate.Body);
        AssertSingle(normalSink, AuthRoute.SignUp, AuthObservationKind.Succeeded);
        AssertSingle(duplicateSink, AuthRoute.SignUp, AuthObservationKind.Succeeded);
    }

    [Fact]
    public async Task BoundedCounterDoesNotChangeAuthenticationResponse()
    {
        var sink = new AuthObservationCounter();
        var response = await Create(sink).Handler(
            Request(AuthRoutes.NewPasswordChallengePath, new
            {
                email = TestEmail, password = TestPassword, session = TestSession
            }), null!);
        Assert.Equal(200, response.StatusCode);
        Assert.Contains(TestToken, response.Body);
        AssertSingle(sink, AuthRoute.NewPasswordChallenge, AuthObservationKind.Succeeded);
    }

    [Fact]
    public async Task ConfirmationCodeCannotBecomeObservationField()
    {
        var sink = new AuthObservationCounter();
        var response = await Create(sink).Handler(
            Request(AuthRoutes.ConfirmSignUpPath, new
            {
                email = TestEmail, code = TestCode
            }), null!);
        Assert.Equal(200, response.StatusCode);
        AssertSingle(sink, AuthRoute.ConfirmSignUp, AuthObservationKind.Succeeded);
    }
    [Fact]
    public void InvalidCounterDimensionsAreIgnored()
    {
        var counter = new AuthObservationCounter();
        counter.Record(new AuthObservation((AuthRoute)int.MaxValue, AuthObservationKind.Accepted));
        counter.Record(new AuthObservation(AuthRoute.Authenticate, (AuthObservationKind)int.MaxValue));
        Assert.Equal(0, counter.TotalCount);
    }

    [Fact]
    public void ConcurrentCounterUpdatesStayWithinFixedCardinality()
    {
        const int IncrementCount = 128;
        var counter = new AuthObservationCounter();
        Parallel.For(0, IncrementCount, _ =>
            counter.Record(new AuthObservation(AuthRoute.Authenticate, AuthObservationKind.Succeeded)));
        Assert.Equal(IncrementCount, counter.TotalCount);
        Assert.Equal(IncrementCount, counter.Count(AuthRoute.Authenticate, AuthObservationKind.Succeeded));
    }

}
