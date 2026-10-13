#nullable enable

using Janus;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Janus.Tests;

public sealed class AccountLifecycleTests
{
    private sealed class MemoryStore : IAccountStore
    {
        public HashSet<string> Subjects { get; } = new(StringComparer.Ordinal);
        public bool FailRead { get; set; }
        public bool FailWrite { get; set; }
        public int Reads { get; private set; }
        public int Writes { get; private set; }

        public Task<bool> ExistsAsync(string subject)
        {
            Reads++;
            if (FailRead) throw new InvalidOperationException("Read unavailable");
            return Task.FromResult(Subjects.Contains(subject));
        }

        public Task CreateIfAbsentAsync(Janus.Model.Account account)
        {
            Writes++;
            if (FailWrite) throw new InvalidOperationException("Write unavailable");
            Subjects.Add(account.Id);
            return Task.CompletedTask;
        }
    }

    private sealed class MemoryPublisher : IAccountPublisher
    {
        public int Sends { get; private set; }
        public Janus.Model.Account? Last { get; private set; }
        public bool Fail { get; set; }

        public Task PublishAsync(Janus.Model.Account account)
        {
            Sends++;
            if (Fail) throw new InvalidOperationException("Queue unavailable");
            Last = account;
            return Task.CompletedTask;
        }
    }

    private static Dictionary<string, string> Claims(string verified = "true") => new()
    {
        ["sub"] = "subject-123",
        ["email"] = "owner@example.invalid",
        ["email_verified"] = verified
    };

    private static JObject Event(string trigger = "PostConfirmation_ConfirmSignUp",
        string verified = "true") => new()
    {
        ["triggerSource"] = trigger,
        ["request"] = new JObject
        {
            ["userAttributes"] = new JObject
            {
                ["sub"] = "subject-123",
                ["email"] = "owner@example.invalid",
                ["email_verified"] = verified
            }
        }
    };

    [Fact]
    public async Task RecoveryRejectsUnverifiedClaimsWithoutIO()
    {
        var store = new MemoryStore();
        var publisher = new MemoryPublisher();
        var result = await new AccountRecoveryService(store, publisher).RecoverAsync(Claims("false"));
        Assert.Equal(AccountRecoveryOutcome.Unverified, result);
        Assert.Equal(0, store.Reads);
        Assert.Equal(0, publisher.Sends);
    }

    [Fact]
    public async Task RecoveryUsesSubjectAndOnlyQueuesWhenMissing()
    {
        var store = new MemoryStore();
        var publisher = new MemoryPublisher();
        var service = new AccountRecoveryService(store, publisher);

        var first = await service.RecoverAsync(Claims());
        Assert.Equal(AccountRecoveryOutcome.Queued, first);
        Assert.Equal("subject-123", publisher.Last!.Id);
        Assert.Equal("owner@example.invalid", publisher.Last.Email);

        await new AccountProvisioningService(store).ProvisionAsync(publisher.Last);
        Assert.Equal(AccountRecoveryOutcome.AlreadyProvisioned, await service.RecoverAsync(Claims()));
        Assert.Equal(1, publisher.Sends);
    }

    [Fact]
    public async Task RecoveryQueueFailurePropagatesFor503()
    {
        var store = new MemoryStore();
        var publisher = new MemoryPublisher { Fail = true };
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => new AccountRecoveryService(store, publisher).RecoverAsync(Claims()));
    }

    [Fact]
    public async Task RecoveryDatabaseFailureDoesNotQueue()
    {
        var store = new MemoryStore { FailRead = true };
        var publisher = new MemoryPublisher();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => new AccountRecoveryService(store, publisher).RecoverAsync(Claims()));
        Assert.Equal(0, publisher.Sends);
    }

    [Fact]
    public async Task ProvisioningRejectsMalformedMessagesWithoutWriting()
    {
        var store = new MemoryStore();
        var service = new AccountProvisioningService(store);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ProvisionAsync(null));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ProvisionAsync(new Janus.Model.Account { Id = "subject-123" }));
        Assert.Equal(0, store.Writes);
    }

    [Fact]
    public async Task ProvisioningPreservesFailureForQueueRetry()
    {
        var store = new MemoryStore { FailWrite = true };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new AccountProvisioningService(store).ProvisionAsync(new Janus.Model.Account
            {
                Id = "subject-123",
                Email = "owner@example.invalid"
            }));
    }

    [Fact]
    public async Task DuplicateProvisioningKeepsFirstRecord()
    {
        var store = new MemoryStore();
        var service = new AccountProvisioningService(store);
        var account = new Janus.Model.Account { Id = "subject-123", Email = "owner@example.invalid" };
        await service.ProvisionAsync(account);
        await service.ProvisionAsync(account);
        Assert.Single(store.Subjects);
        Assert.Equal("subject-123", Assert.Single(store.Subjects));
    }

    [Fact]
    public async Task NonSignupTriggerNeverPublishes()
    {
        var publisher = new MemoryPublisher();
        var e = Event("PostConfirmation_ConfirmForgotPassword");
        var result = await new AccountConfirmed(publisher).Handler(e, null!);
        Assert.Same(e, result);
        Assert.Equal(0, publisher.Sends);
    }

    [Fact]
    public async Task UnverifiedSignupFailsClosedBeforePublish()
    {
        var publisher = new MemoryPublisher();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => new AccountConfirmed(publisher).Handler(Event(verified: "false"), null!));
        Assert.Equal(0, publisher.Sends);
    }

    [Fact]
    public async Task VerifiedSignupPublishesAndReturnsOriginalEvent()
    {
        var publisher = new MemoryPublisher();
        var e = Event();
        var returned = await new AccountConfirmed(publisher).Handler(e, null!);
        Assert.Same(e, returned);
        Assert.Equal(1, publisher.Sends);
        Assert.Equal("subject-123", publisher.Last!.Id);
    }

    [Fact]
    public async Task FailedConfirmedPublishDoesNotAcknowledgeSuccess()
    {
        var publisher = new MemoryPublisher { Fail = true };
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => new AccountConfirmed(publisher).Handler(Event(), null!));
    }
}
