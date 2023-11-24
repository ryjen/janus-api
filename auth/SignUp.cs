
using Amazon.Lambda.APIGatewayEvents;
using Amazon.CognitoIdentityProvider.Model;

namespace Janus;

using static Shared;
using Account = Janus.Model.Account;
using RequestParams = Dictionary<string, string>;

public partial class Auth
{
  private readonly Database _db = new Database();

  public async Task<APIGatewayProxyResponse> SignUp(RequestParams request)
  {
	try
	{
	  string email = request["email"];
	  string password = request["password"];

	  var attributes = new List<AttributeType>();
	  var deliveryMediums = new List<string>();

	  attributes.Add(new AttributeType { Name = "email", Value = email });
	  deliveryMediums.Add("EMAIL");

	  var authRequest = new AdminCreateUserRequest
	  {
		UserPoolId = userPoolId,
		Username = email,
		TemporaryPassword = password,
		MessageAction = "SUPPRESS",
		UserAttributes = attributes,
		DesiredDeliveryMediums = deliveryMediums,
	  };
	  var authResponse = await _cognitoClient.AdminCreateUserAsync(authRequest);

	  var sub = authResponse.User.Attributes.Find(x => x.Name == "sub").Value;

	  var account = new Account { Id = sub, Email = email };

	  await _db.Save(account);

	  return Response(200, account);
	}
	catch (Exception ex)
	{
	  // TODO: handle rollback
	  return Response(401, new { Message = "Authentication failed", Error = ex.Message });
	}
  }
}
