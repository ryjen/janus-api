#nullable enable

namespace Janus;

// Application ports: implementations may be AWS adapters, deterministic fakes,
// or alternative transports. No SQS or DynamoDB types cross these contracts.
public interface IAccountPublisher
{
    Task PublishAsync(Model.Account account);
}

public interface IAccountStore
{
    Task<bool> ExistsAsync(string subject);
    Task CreateIfAbsentAsync(Model.Account account);
}

public enum AccountRecoveryOutcome
{
    Unverified,
    AlreadyProvisioned,
    Queued
}

public sealed class AccountRecoveryService
{
    private readonly IAccountStore _store;
    private readonly IAccountPublisher _publisher;

    public AccountRecoveryService(IAccountStore store, IAccountPublisher publisher)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
    }

    public async Task<AccountRecoveryOutcome> RecoverAsync(IDictionary<string, string>? verifiedClaims)
    {
        var account = AccountRecoveryPolicy.VerifiedAccount(verifiedClaims);
        if (account is null) return AccountRecoveryOutcome.Unverified;
        if (await _store.ExistsAsync(account.Id)) return AccountRecoveryOutcome.AlreadyProvisioned;

        // A send failure propagates to the HTTP adapter as 503; it must not
        // accidentally be counted as successful provisioning.
        await _publisher.PublishAsync(account);
        return AccountRecoveryOutcome.Queued;
    }
}

public sealed class AccountProvisioningService
{
    private readonly IAccountStore _store;

    public AccountProvisioningService(IAccountStore store) =>
        _store = store ?? throw new ArgumentNullException(nameof(store));

    public Task ProvisionAsync(Model.Account? account)
    {
        if (account is null || string.IsNullOrWhiteSpace(account.Id) ||
            string.IsNullOrWhiteSpace(account.Email))
            throw new InvalidOperationException("Incomplete account provisioning event");

        // The store implements conditional-create. Propagate failure so SQS
        // redelivery and DLQ behavior are preserved.
        return _store.CreateIfAbsentAsync(account);
    }
}
