using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Janus;

public sealed class AccountConfirmed
{
    private readonly IAmazonSQS _sqs = new AmazonSQSClient();

    public async Task<JObject> Handler(JObject cognitoEvent, ILambdaContext context)
    {
        var account = VerifiedEnrollment.FromCognitoEvent(cognitoEvent);
        if (account is null)
            return cognitoEvent; // For example, a password-reset confirmation event.

        var url = Environment.GetEnvironmentVariable("QUEUE_URL");
        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException("Queue not configured");

        await _sqs.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = url,
            MessageBody = JsonConvert.SerializeObject(account)
        });

        return cognitoEvent; // Cognito requires the original event in the response.
    }
}
