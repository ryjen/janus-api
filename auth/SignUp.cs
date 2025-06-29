
using Amazon.Lambda.APIGatewayEvents;
using Amazon.CognitoIdentityProvider.Model;
using static System.Net.HttpStatusCode;
using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;

namespace Janus;

using static Shared;
using AccountModel = Janus.Model.Account;
using RequestParams = Dictionary<string, object>;

public partial class Auth
{
    private readonly Database _db = new Database();
    private readonly IAmazonSQS _sqs = new AmazonSQSClient();
    private readonly string _sqsUrl = Environment.GetEnvironmentVariable("QUEUE_URL");

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
                UserPoolId = _userPoolId,
                Username = email,
                TemporaryPassword = password,
                MessageAction = "SUPPRESS",
                UserAttributes = attributes,
                DesiredDeliveryMediums = deliveryMediums,
            };
            var authResponse = await _cognitoClient.AdminCreateUserAsync(authRequest);

            var sub = authResponse.User.Attributes.Find(x => x.Name == "sub").Value;
            var account = new AccountModel { Id = sub, Email = email };

            await SendNewAccount(account);

            return Response(OK, account);
        }
        catch
        {
            // TODO: handle rollback
            return Response(Unauthorized, new { Message = "Authentication failed" });
        }
    }

    private async Task SendNewAccount(object message)
    {
        var request = new SendMessageRequest
        {
            QueueUrl = _sqsUrl,
            MessageBody = JsonConvert.SerializeObject(message)

        };

        log(string.Format("sending {0} to {1}", message, _sqsUrl));

        await _sqs.SendMessageAsync(request);
    }
}
