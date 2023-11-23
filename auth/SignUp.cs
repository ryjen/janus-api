
using Amazon.Lambda.APIGatewayEvents;
using Amazon.CognitoIdentityProvider.Model;

namespace Janus;

using static Shared;
using RequestParams = Dictionary<string, string>;

public partial class Auth
{
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

            // TODO: handle errors and rollback
            Account.CreateAccount(new { Id = sub, Email = email});

            // TODO: use models
            return Response(200, new { AccountID = sub, Email = email });
        }
        catch (Exception ex)
        {
            return Response(401, new { Message = "Authentication failed", Error = ex.Message });
        }
    }
}
