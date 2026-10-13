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
    {
        _cognitoClient = cognitoClient ?? throw new ArgumentNullException(nameof(cognitoClient));
        _userPoolId = !string.IsNullOrWhiteSpace(userPoolId) ? userPoolId :
            throw new ArgumentException("User pool configuration required", nameof(userPoolId));
        _clientId = !string.IsNullOrWhiteSpace(clientId) ? clientId :
            throw new ArgumentException("Client configuration required", nameof(clientId));
    }

    public async Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            if (request.Path is not ("/authenticate" or "/reset" or "/signup" or "/confirm" or
                "/password/forgot" or "/password/confirm"))
                return Response(Unauthorized, new { Message = "Authentication failed", Error = "unknown path" });

            if (!RecordRequestParser.TryParseFields(request.Body, out var parameters))
                return Response(BadRequest, new { Message = "Invalid JSON object" });

            switch (request.Path)
            {
            case "/authenticate":
                return await Authenticate(parameters!);
            case "/reset":
                return await NewPassword(parameters!);
            case "/signup":
                return await SignUp(parameters!);
            case "/confirm":
                return await ConfirmSignUp(parameters!);
            case "/password/forgot":
                return await StartPasswordRecovery(parameters!);
            case "/password/confirm":
                return await ConfirmPasswordRecovery(parameters!);
            default:
                return Response(Unauthorized, new { Message = "Authentication failed", Error = "unknown path" });
            }
        }
        catch
        {
            return Response(InternalServerError, new { Message = "Internal error" });
        }
    }
}
