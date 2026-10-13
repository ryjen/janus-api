#nullable enable

using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;

namespace Janus;

// Shared AWS adapters live beside the existing Database SDK wrapper for now.
// Move to an infrastructure assembly after removing the historical duplicate
// auth/account assembly identity; application ports remain AWS-independent.
public sealed class SqsAccountPublisher : IAccountPublisher
{
    private readonly IAmazonSQS _sqs;
    private readonly string? _queueUrl;

    public SqsAccountPublisher()
        : this(new AmazonSQSClient(), Environment.GetEnvironmentVariable("QUEUE_URL")) { }

    public SqsAccountPublisher(IAmazonSQS sqs, string? queueUrl)
    {
        _sqs = sqs ?? throw new ArgumentNullException(nameof(sqs));
        _queueUrl = queueUrl;
    }

    public async Task PublishAsync(Model.Account account)
    {
        if (string.IsNullOrWhiteSpace(_queueUrl))
            throw new InvalidOperationException("Account queue is not configured");
        if (account is null || string.IsNullOrWhiteSpace(account.Id) ||
            string.IsNullOrWhiteSpace(account.Email))
            throw new InvalidOperationException("Verified account is required");

        await _sqs.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = _queueUrl,
            MessageBody = JsonConvert.SerializeObject(account)
        });
    }
}

public sealed class DynamoAccountStore : IAccountStore
{
    private readonly Database _database;
    private readonly string _table;

    public DynamoAccountStore()
        : this(new Database(), Environment.GetEnvironmentVariable("ACCOUNT_TABLE")) { }

    public DynamoAccountStore(Database database, string? table)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _table = !string.IsNullOrWhiteSpace(table) ? table :
            throw new InvalidOperationException("Account table is not configured");
    }

    public async Task<bool> ExistsAsync(string subject) =>
        await _database.Read(new ReadRequest { Entity = _table, Id = subject }) is not null;

    public Task CreateIfAbsentAsync(Model.Account account) =>
        _database.CreateIfAbsent(new CreateRequest
        {
            Entity = _table,
            Data = account
        }, account.Id);
}
