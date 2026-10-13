using Amazon.Lambda.SQSEvents;
using Newtonsoft.Json;
using AccountModel = Janus.Model.Account;

namespace Janus;

// Separate queue entry point; the account HTTP query handler cannot interpret
// SQS events. Invalid messages and store faults throw, allowing retry/DLQ.
public sealed class AccountProvisioner
{
    private readonly AccountProvisioningService _provisioning;

    public AccountProvisioner()
        : this(new AccountProvisioningService(new DynamoAccountStore())) { }

    public AccountProvisioner(AccountProvisioningService provisioning) =>
        _provisioning = provisioning ?? throw new ArgumentNullException(nameof(provisioning));

    public async Task Handler(SQSEvent sqs, ILambdaContext context)
    {
        foreach (var record in sqs.Records)
        {
            var account = JsonConvert.DeserializeObject<AccountModel>(record.Body);
            await _provisioning.ProvisionAsync(account);
        }
    }
}
