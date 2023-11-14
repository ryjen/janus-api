
using Amazon.Lambda.APIGatewayEvents;
using Newtonsoft.Json;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using System.Threading.Tasks;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Janus;

public class ImmutableAuth
{
  private readonly AmazonCognitoIdentityProviderClient _cognitoClient = new AmazonCognitoIdentityProviderClient();

  public async Task<APIGatewayProxyResponse> Authenticate(APIGatewayProxyRequest request, ILambdaContext context)
  {
	var requestBody = JsonConvert.DeserializeObject<Dictionary<string, string>>(request.Body);
	string username = requestBody["username"];
	string password = requestBody["password"];

	// Perform authentication using Cognito User Pool
	var authenticationRequest = new AdminInitiateAuthRequest
	{
	  UserPoolId = Environment.GetEnvironmentVariable("USER_POOL_ID"),
	  ClientId = Environment.GetEnvironmentVariable("CLIENT_ID"),
	  AuthFlow = AuthFlowType.ADMIN_NO_SRP_AUTH,
	  AuthParameters = new Dictionary<string, string>
			{
				{"USERNAME", username},
				{"PASSWORD", password}
			}
	};

	try
	{
	  var authResponse = await _cognitoClient.AdminInitiateAuthAsync(authenticationRequest);
	  string token = authResponse.AuthenticationResult.IdToken;

	  return new APIGatewayProxyResponse
	  {
		StatusCode = 200,
		Body = JsonConvert.SerializeObject(new { Token = token }),
		Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
	  };
	}
	catch (Exception ex)
	{
	  return new APIGatewayProxyResponse
	  {
		StatusCode = 401,
		Body = JsonConvert.SerializeObject(new { Message = "Authentication failed", Error = ex.Message }),
		Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
	  };
	}
  }
}
