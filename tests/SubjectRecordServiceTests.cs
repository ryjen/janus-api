#nullable enable

using Janus;
using Xunit;

namespace Janus.Tests;

public sealed class SubjectRecordServiceTests
{
    private sealed class FakeStore : IRecordStore
    {
        public string? Json { get; set; }
        public string? LastId { get; private set; }
        public string? LastAction { get; private set; }
        public IReadOnlyDictionary<string, object>? LastFields { get; private set; }
        public int Calls { get; private set; }
        public bool Fail { get; set; }

        public Task<string?> ReadJsonAsync(string subject)
        {
            Track("read", subject);
            return Task.FromResult(Json);
        }

        public Task UpsertAsync(string subject, IReadOnlyDictionary<string, object> fields)
        {
            Track("write", subject);
            LastFields = fields;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string subject)
        {
            Track("delete", subject);
            return Task.CompletedTask;
        }

        private void Track(string action, string subject)
        {
            Calls++;
            LastAction = action;
            LastId = subject;
            if (Fail) throw new InvalidOperationException("Simulated dependency outage");
        }
    }

    [Fact]
    public async Task ReadingUsesOnlyValidatedSubject()
    {
        var store = new FakeStore { Json = "{\"id\":\"owner\",\"data\":{}}" };
        var result = await new SubjectRecordService(store).ReadAsync("owner", null);
        Assert.Equal(RecordStatus.Ok, result.Status);
        Assert.Equal(store.Json, result.Json);
        Assert.Equal("owner", store.LastId);
        Assert.Equal("read", store.LastAction);
    }

    [Fact]
    public async Task MissingRecordsAreNotReportedAsSuccess()
    {
        var result = await new SubjectRecordService(new FakeStore()).ReadAsync("owner", "owner");
        Assert.Equal(RecordStatus.Missing, result.Status);
    }

    [Theory]
    [InlineData(null, null, RecordStatus.Unauthorized)]
    [InlineData("", null, RecordStatus.Unauthorized)]
    [InlineData("owner", "another", RecordStatus.Forbidden)]
    [InlineData("owner", "", RecordStatus.Forbidden)]
    [InlineData("owner", "  ", RecordStatus.Forbidden)]
    [InlineData("owner", "OWNER", RecordStatus.Forbidden)]
    public async Task UnauthorizedReadsNeverTouchStore(string? subject, string? requestedId, RecordStatus expected)
    {
        var store = new FakeStore();
        var result = await new SubjectRecordService(store).ReadAsync(subject, requestedId);
        Assert.Equal(expected, result.Status);
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task WriteDropsIdWithoutMutatingCallerInput()
    {
        var fields = new Dictionary<string, object> { ["id"] = "owner", ["level"] = 2 };
        var store = new FakeStore();
        var result = await new SubjectRecordService(store).UpsertAsync("owner", fields);
        Assert.Equal(RecordStatus.Ok, result.Status);
        Assert.Equal("owner", store.LastId);
        Assert.False(store.LastFields!.ContainsKey("id"));
        Assert.Equal(2, store.LastFields["level"]);
        Assert.True(fields.ContainsKey("id"));
    }

    [Theory]
    [InlineData("stranger")]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ContradictoryWriteIdsNeverReachStore(string requestedId)
    {
        var store = new FakeStore();
        var result = await new SubjectRecordService(store).UpsertAsync(
            "owner", new Dictionary<string, object> { ["id"] = requestedId });
        Assert.Equal(RecordStatus.Forbidden, result.Status);
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task StructuredIdCannotBeCoercedToSubject()
    {
        var store = new FakeStore();
        var result = await new SubjectRecordService(store).UpsertAsync(
            "owner", new Dictionary<string, object> { ["id"] = new { value = "owner" } });
        Assert.Equal(RecordStatus.Forbidden, result.Status);
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task ExplicitNullIdIsRejected()
    {
        var store = new FakeStore();
        var result = await new SubjectRecordService(store).DeleteAsync(
            "owner", new Dictionary<string, object> { ["id"] = null! });
        Assert.Equal(RecordStatus.Forbidden, result.Status);
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task DeleteUsesSubjectAndIgnoresOtherFields()
    {
        var store = new FakeStore();
        var result = await new SubjectRecordService(store).DeleteAsync(
            "owner", new Dictionary<string, object> { ["id"] = "owner", ["entity"] = "Admin" });
        Assert.Equal(RecordStatus.Ok, result.Status);
        Assert.Equal("owner", store.LastId);
    }

    [Fact]
    public async Task ReadOnlyStoreDeniesMutationBeforePersistence()
    {
        var store = new FakeStore();
        var service = new SubjectRecordService(store, readOnly: true);
        Assert.Equal(RecordStatus.ReadOnly,
            (await service.UpsertAsync("owner", new Dictionary<string, object>())).Status);
        Assert.Equal(RecordStatus.ReadOnly,
            (await service.DeleteAsync("owner", new Dictionary<string, object>())).Status);
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task BackendFailureIsNotSuppressedInApplicationLayer()
    {
        var store = new FakeStore { Fail = true };
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => new SubjectRecordService(store).ReadAsync("owner", null));
    }

    [Fact]
    public async Task TypedUpsertEnforcesSubjectAndRemovesId()
    {
        var store = new FakeStore();
        var fields = new Dictionary<string, object> { ["id"] = "owner", ["level"] = 7 };
        var result = await new SubjectRecordService(store).UpsertAsync(
            "owner", new RecordMutationRequest(fields));
        Assert.Equal(RecordStatus.Ok, result.Status);
        Assert.Equal("owner", store.LastId);
        Assert.Equal(7, store.LastFields!["level"]);
        Assert.False(store.LastFields.ContainsKey("id"));
        Assert.True(fields.ContainsKey("id"));
    }

    [Fact]
    public async Task TypedDeleteRejectsCrossSubjectWithoutPersistence()
    {
        var store = new FakeStore();
        var result = await new SubjectRecordService(store).DeleteAsync(
            "owner", new RecordMutationRequest(new Dictionary<string, object> { ["id"] = "another" }));
        Assert.Equal(RecordStatus.Forbidden, result.Status);
        Assert.Equal(0, store.Calls);
    }
}
