
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
            switch (request.Path)
            {
            case "/authenticate":
                return await Authenticate(request.Body.ToParams());
            case "/reset":
                return await NewPassword(request.Body.ToParams());
            case "/signup":
                return await SignUp(request.Body.ToParams());
            case "/confirm":
                return await ConfirmSignUp(request.Body.ToParams());
            case "/password/forgot":
                return await StartPasswordRecovery(request.Body.ToParams());
            case "/password/confirm":
                return await ConfirmPasswordRecovery(request.Body.ToParams());
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
