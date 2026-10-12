using Amazon.Lambda.APIGatewayEvents;
using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;
using static System.Net.HttpStatusCode;

namespace Janus;

using static Shared;
using AccountModel = Janus.Model.Account;

public sealed class AccountRecovery
{
    // A confirmed Cognito user can recover an account when the asynchronous
    // PostConfirmation -> SQS link fails. There is no caller-supplied subject.
    public async Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var claims = request.RequestContext?.Authorizer?.Claims;
        if (claims == null || !claims.TryGetValue("sub", out var subject) || string.IsNullOrWhiteSpace(subject))
            return Response(Unauthorized, new { Message = "Authentication required" });

        var account = AccountRecoveryPolicy.VerifiedAccount(claims);
        if (account is null)
            return Response(Forbidden, new { Message = "Verified email required" });

        if (request.HttpMethod != "POST")
            return Response(MethodNotAllowed, new { Message = "Invalid method" });

        var queue = Environment.GetEnvironmentVariable("QUEUE_URL");
        var table = Environment.GetEnvironmentVariable("ACCOUNT_TABLE");
        if (string.IsNullOrWhiteSpace(queue) || string.IsNullOrWhiteSpace(table))
            return Response(ServiceUnavailable, new { Message = "Account recovery unavailable" });

        try
        {
            var existing = await new Database().Read(new ReadRequest
            {
                Entity = table,
                Id = account.Id
            });
            if (existing != null)
                return Response(OK, new { Message = "Account already provisioned" });

            await new AmazonSQSClient().SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = queue,
                MessageBody = JsonConvert.SerializeObject(account)
            });

            return Response(Accepted, new { Message = "Account provisioning queued" });
        }
        catch
        {
            return Response(ServiceUnavailable, new { Message = "Account recovery unavailable" });
        }
    }
}
