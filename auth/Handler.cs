#nullable enable
using Amazon.Lambda.APIGatewayEvents;
using Amazon.CognitoIdentityProvider;
using static System.Net.HttpStatusCode;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Janus;

using static Shared;

public partial class Auth
{

    // Parameterless Lambda composition root; injected client is used in tests.
    private readonly IAmazonCognitoIdentityProvider _cognitoClient;
    private readonly IEnrollmentGateway _enrollmentGateway;
    private readonly IPasswordRecoveryGateway _passwordRecoveryGateway;
    private readonly AuthenticationService _authenticationService;
    private readonly IAuthObservationSink _observations;

    private readonly string _userPoolId;
    private readonly string _clientId;

    public Auth() : this(
        new AmazonCognitoIdentityProviderClient(),
        Environment.GetEnvironmentVariable("USER_POOL_ID"),
        Environment.GetEnvironmentVariable("CLIENT_ID")) { }

    public Auth(IAmazonCognitoIdentityProvider cognitoClient)
        : this(cognitoClient,
            Environment.GetEnvironmentVariable("USER_POOL_ID"),
            Environment.GetEnvironmentVariable("CLIENT_ID")) { }

    public Auth(IAmazonCognitoIdentityProvider cognitoClient, string? userPoolId, string? clientId)
        : this(cognitoClient, userPoolId, clientId, enrollmentGateway: null) { }

    // Inject the enrollment port for contract tests without replacing the
    // existing default Lambda composition and Cognito-client constructors.
    // Preserve this exact public constructor signature: optional arguments are
    // resolved at compile time, so replacing it would break existing binaries.
    public Auth(IAmazonCognitoIdentityProvider cognitoClient, string? userPoolId,
        string? clientId, IEnrollmentGateway? enrollmentGateway,
        IPasswordRecoveryGateway? passwordRecoveryGateway = null,
        IAuthenticationGateway? authenticationGateway = null)
        : this(cognitoClient, userPoolId, clientId, enrollmentGateway,
            passwordRecoveryGateway, authenticationGateway, observations: null) { }

    // Opt-in observation sink is a distinct overload; existing binaries and
    // the default Lambda composition continue to resolve the original method.
    public Auth(IAmazonCognitoIdentityProvider cognitoClient, string? userPoolId,
        string? clientId, IEnrollmentGateway? enrollmentGateway,
        IPasswordRecoveryGateway? passwordRecoveryGateway,
        IAuthenticationGateway? authenticationGateway,
        IAuthObservationSink? observations)
    {
        _cognitoClient = cognitoClient ?? throw new ArgumentNullException(nameof(cognitoClient));
        _userPoolId = !string.IsNullOrWhiteSpace(userPoolId) ? userPoolId :
            throw new ArgumentException("User pool configuration required", nameof(userPoolId));
        _clientId = !string.IsNullOrWhiteSpace(clientId) ? clientId :
            throw new ArgumentException("Client configuration required", nameof(clientId));
        _enrollmentGateway = enrollmentGateway ?? new CognitoEnrollmentGateway(_cognitoClient, _clientId);
        _passwordRecoveryGateway = passwordRecoveryGateway ??
            new CognitoPasswordRecoveryGateway(_cognitoClient, _clientId);
        _authenticationService = new AuthenticationService(
            authenticationGateway ?? new CognitoAuthenticationGateway(
                _cognitoClient, _userPoolId, _clientId));
        _observations = observations ?? NoOpAuthObservationSink.Instance;
    }

    public async Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var route = AuthRoute.Unknown;
        APIGatewayProxyResponse result;
        try
        {
            route = AuthRoutes.Resolve(request.Path);
            if (route is AuthRoute.Unknown)
                result = Response(Unauthorized, new { Message = "Authentication failed", Error = "unknown path" });
            else if (!RecordRequestParser.TryParseFields(request.Body, out var parameters))
                result = Response(BadRequest, new { Message = "Invalid JSON object" });
            else
                result = await DispatchAsync(route, parameters!);
        }
        catch
        {
            result = Response(InternalServerError, new { Message = "Internal error" });
        }

        // The only allowed observation dimensions are finite enums, derived
        // from trusted dispatch/status codes, never request or response bodies.
        // A failing optional sink must not change authentication outcomes.
        try
        {
            _observations.Record(new AuthObservation(route, ClassifyStatus(result.StatusCode)));
        }
        catch
        {
            // Deliberately do not log sink failures or request/response details.
        }

        return result;
    }

    private Task<APIGatewayProxyResponse> DispatchAsync(
        AuthRoute route, Dictionary<string, object> parameters) => route switch
    {
        AuthRoute.Authenticate => Authenticate(parameters),
        AuthRoute.NewPasswordChallenge => NewPassword(parameters),
        AuthRoute.SignUp => SignUp(parameters),
        AuthRoute.ConfirmSignUp => ConfirmSignUp(parameters),
        AuthRoute.ForgotPassword => StartPasswordRecovery(parameters),
        AuthRoute.ConfirmForgotPassword => ConfirmPasswordRecovery(parameters),
        _ => Task.FromResult(Response(Unauthorized, new
        {
            Message = "Authentication failed",
            Error = "unknown path"
        }))
    };

    private static AuthObservationKind ClassifyStatus(int statusCode) =>
        (System.Net.HttpStatusCode)statusCode switch
        {
            OK => AuthObservationKind.Succeeded,
            Accepted => AuthObservationKind.Accepted,
            BadRequest => AuthObservationKind.InvalidInput,
            Unauthorized => AuthObservationKind.DeniedOrChallenged,
            ServiceUnavailable => AuthObservationKind.Unavailable,
            _ => AuthObservationKind.Failed
        };
}
