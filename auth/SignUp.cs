
using Amazon.Lambda.APIGatewayEvents;
using Amazon.CognitoIdentityProvider.Model;
using static System.Net.HttpStatusCode;

namespace Janus;

using static Shared;
using RequestParams = Dictionary<string, object>;

public partial class Auth
{
    private readonly Database _db = new Database();

    public async Task<APIGatewayProxyResponse> SignUp(RequestParams request)
    {
        try
        {
            string email = request["email"].ToString();
            string password = request["password"].ToString();

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

            var account = new UpdateRequest
            {
                Id = sub,
                Entity = "Account",
                Data = new
                {
                    Email = email
                }
            };

            // TODO: use SQS events
            await _db.Update(account);

            return Response(OK, account);
        }
        catch
        {
            // TODO: handle rollback
            return Response(Unauthorized, new { Message = "Authentication failed" });
        }
    }
}
