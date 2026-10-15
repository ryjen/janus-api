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
    public Auth(IAmazonCognitoIdentityProvider cognitoClient, string? userPoolId,
        string? clientId, IEnrollmentGateway? enrollmentGateway,
        IPasswordRecoveryGateway? passwordRecoveryGateway = null)
    {
        _cognitoClient = cognitoClient ?? throw new ArgumentNullException(nameof(cognitoClient));
        _userPoolId = !string.IsNullOrWhiteSpace(userPoolId) ? userPoolId :
            throw new ArgumentException("User pool configuration required", nameof(userPoolId));
        _clientId = !string.IsNullOrWhiteSpace(clientId) ? clientId :
            throw new ArgumentException("Client configuration required", nameof(clientId));
        _enrollmentGateway = enrollmentGateway ?? new CognitoEnrollmentGateway(_cognitoClient, _clientId);
        _passwordRecoveryGateway = passwordRecoveryGateway ??
            new CognitoPasswordRecoveryGateway(_cognitoClient, _clientId);
    }

    public async Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            var route = AuthRoutes.Resolve(request.Path);
            if (route is AuthRoute.Unknown)
                return Response(Unauthorized, new { Message = "Authentication failed", Error = "unknown path" });

            if (!RecordRequestParser.TryParseFields(request.Body, out var parameters))
                return Response(BadRequest, new { Message = "Invalid JSON object" });

            return route switch
            {
                AuthRoute.Authenticate => await Authenticate(parameters!),
                AuthRoute.NewPasswordChallenge => await NewPassword(parameters!),
                AuthRoute.SignUp => await SignUp(parameters!),
                AuthRoute.ConfirmSignUp => await ConfirmSignUp(parameters!),
                AuthRoute.ForgotPassword => await StartPasswordRecovery(parameters!),
                AuthRoute.ConfirmForgotPassword => await ConfirmPasswordRecovery(parameters!),
                _ => Response(Unauthorized, new { Message = "Authentication failed", Error = "unknown path" })
            };
        }
        catch
        {
            return Response(InternalServerError, new { Message = "Internal error" });
        }
    }
}
