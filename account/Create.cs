using Amazon.Lambda.SQSEvents;
using Newtonsoft.Json;

namespace Janus;

using AccountModel = Janus.Model.Account;

public partial class Account
{
    // Dedicated SQS handler: an API Gateway proxy request cannot deserialize SQS records.
    public async Task Provision(SQSEvent sqs, ILambdaContext context)
    {
        var table = Environment.GetEnvironmentVariable("ACCOUNT_TABLE")
            ?? throw new InvalidOperationException("ACCOUNT_TABLE is not configured");
        var db = new Database();

        foreach (var record in sqs.Records)
        {
            var account = JsonConvert.DeserializeObject<AccountModel>(record.Body)
                ?? throw new InvalidOperationException("Invalid account creation message");
            if (string.IsNullOrWhiteSpace(account.Id) || string.IsNullOrWhiteSpace(account.Email))
                throw new InvalidOperationException("Incomplete account creation message");

            await db.Create(new CreateRequest { Entity = table, Data = account }, account.Id);
        }
    }
}
