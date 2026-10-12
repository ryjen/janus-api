#nullable enable

namespace Janus;

// DynamoDB adapter. Resource selection is fixed at construction from trusted
// Lambda configuration, never from HTTP request body/query parameters.
public sealed class DynamoRecordStore : IRecordStore
{
    private readonly Database _database;
    private readonly string _table;

    public DynamoRecordStore(string entity, Database? database = null)
    {
        if (string.IsNullOrWhiteSpace(entity))
            throw new ArgumentException("Record collection is required", nameof(entity));

        _table = Environment.GetEnvironmentVariable(entity.ToUpperInvariant() + "_TABLE")
            ?? throw new InvalidOperationException("Record table configuration is missing");
        _database = database ?? new Database();
    }

    public async Task<string?> ReadJsonAsync(string subject)
    {
        var doc = await _database.Read(new ReadRequest { Entity = _table, Id = subject });
        return doc?.ToJson();
    }

    public Task UpsertAsync(string subject, IReadOnlyDictionary<string, object> fields) =>
        _database.Update(new UpdateRequest
        {
            Entity = _table,
            Id = subject,
            Data = fields
        });

    public Task DeleteAsync(string subject) =>
        _database.Delete(new DeleteRequest { Entity = _table, Id = subject });
}
