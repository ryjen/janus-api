#nullable enable

namespace Janus;

// Application port: bound to exactly one record collection by the composition root.
// Keeping AWS SDK types out of this contract permits deterministic tests.
public interface IRecordStore
{
    Task<string?> ReadJsonAsync(string subject);
    Task UpsertAsync(string subject, IReadOnlyDictionary<string, object> fields);
    Task DeleteAsync(string subject);
}

public enum RecordStatus
{
    Ok,
    Missing,
    Unauthorized,
    Forbidden,
    ReadOnly
}

public sealed record RecordResult(RecordStatus Status, string? Json = null);

// Application service: enforces the subject invariant before touching persistence.
// An optional caller ID can confirm identity; it can never redirect the operation.
public sealed class SubjectRecordService
{
    private readonly IRecordStore _store;
    private readonly bool _readOnly;

    public SubjectRecordService(IRecordStore store, bool readOnly = false)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _readOnly = readOnly;
    }

    public bool IsReadOnly => _readOnly;

    public async Task<RecordResult> ReadAsync(string? subject, string? requestedId)
    {
        var access = Authorize(subject, requestedId);
        if (access != RecordStatus.Ok) return new RecordResult(access);

        var json = await _store.ReadJsonAsync(subject!);
        return json is null
            ? new RecordResult(RecordStatus.Missing)
            : new RecordResult(RecordStatus.Ok, json);
    }

    // Typed application boundary; retain dictionary overload for existing callers.
    public Task<RecordResult> UpsertAsync(string? subject, RecordMutationRequest request) =>
        UpsertAsync(subject, request?.Fields ?? throw new ArgumentNullException(nameof(request)));

    public Task<RecordResult> DeleteAsync(string? subject, RecordMutationRequest request) =>
        DeleteAsync(subject, request?.Fields ?? throw new ArgumentNullException(nameof(request)));

    public async Task<RecordResult> UpsertAsync(string? subject, IReadOnlyDictionary<string, object> fields)
    {
        if (string.IsNullOrWhiteSpace(subject)) return new RecordResult(RecordStatus.Unauthorized);
        if (_readOnly) return new RecordResult(RecordStatus.ReadOnly);
        if (fields is null) throw new ArgumentNullException(nameof(fields));

        if (!AllowedBodyId(subject, fields)) return new RecordResult(RecordStatus.Forbidden);

        // The repository never receives a caller-selected key. Copy the body so
        // neither the store nor the caller can mutate the original request map.
        var data = fields.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        data.Remove("id");
        await _store.UpsertAsync(subject, data);
        return new RecordResult(RecordStatus.Ok);
    }

    public async Task<RecordResult> DeleteAsync(string? subject, IReadOnlyDictionary<string, object> fields)
    {
        if (string.IsNullOrWhiteSpace(subject)) return new RecordResult(RecordStatus.Unauthorized);
        if (_readOnly) return new RecordResult(RecordStatus.ReadOnly);
        if (fields is null) throw new ArgumentNullException(nameof(fields));
        if (!AllowedBodyId(subject, fields)) return new RecordResult(RecordStatus.Forbidden);

        await _store.DeleteAsync(subject);
        return new RecordResult(RecordStatus.Ok);
    }

    private static RecordStatus Authorize(string? subject, string? requestedId)
    {
        if (string.IsNullOrWhiteSpace(subject)) return RecordStatus.Unauthorized;
        return requestedId is null || string.Equals(subject, requestedId, StringComparison.Ordinal)
            ? RecordStatus.Ok : RecordStatus.Forbidden;
    }

    private static bool AllowedBodyId(string subject, IReadOnlyDictionary<string, object> fields)
    {
        if (!fields.TryGetValue("id", out var value)) return true;
        // Explicit null / numeric / structured IDs must not be coerced into a key.
        return value is string requestedId && string.Equals(subject, requestedId, StringComparison.Ordinal);
    }
}
