
using Amazon.Lambda.APIGatewayEvents;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;

namespace Janus;

using static Shared;
using RequestParams = Dictionary<string, string>;

public partial class Auth
{
    private readonly Database _db = new Database();
    private readonly IAmazonSQS _sqs = new AmazonSQSClient();
    private readonly string _sqsUrl = Environment.GetEnvironmentVariable("QUEUE_URL");

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
                UserPoolId = _userPoolId,
                Username = email,
                TemporaryPassword = password,
                MessageAction = "SUPPRESS",
                UserAttributes = attributes,
                DesiredDeliveryMediums = deliveryMediums,
            };
            var authResponse = await _cognitoClient.AdminCreateUserAsync(authRequest);

            var sub = authResponse.User.Attributes.Find(x => x.Name == "sub").Value;
            var account = new { Id = sub, Email = email };

            await SendNewAccount(account);

            return Response(200, account);
        }
        catch (Exception ex)
        {
            // TODO: handle rollback
            return Response(401, new { Message = "Authentication failed", Error = ex.Message });
        }
    }

    private async Task SendNewAccount(object message)
    {
        var request = new SendMessageRequest
        {
            QueueUrl = _sqsUrl,
            MessageBody = JsonConvert.SerializeObject(message)

        };
        await _sqs.SendMessageAsync(request);
    }
}
