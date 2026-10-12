
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2;

namespace Janus;

public class Database
{
    public Database()
    {
        this._client = new AmazonDynamoDBClient();
    }

    protected readonly AmazonDynamoDBClient _client;

    public async Task Update(UpdateRequest request)
    {
        var data = Document.FromJson(request.Data.JsonSerialize());
        var doc = new Document();
        doc.Add("data", data);
        var table = Table.LoadTable(_client, request.Entity);
        await table.UpdateItemAsync(doc, request.Id);
    }

    public async Task<string> Create(CreateRequest request, string? subjectId = null)
    {
        var data = Document.FromJson(request.Data.JsonSerialize());
        var doc = new Document();
        var id = subjectId ?? Guid.NewGuid().ToString();
        doc.Add("id", id);
        doc.Add("data", data);
        var table = Table.LoadTable(_client, request.Entity);
        await table.PutItemAsync(doc);
        return id;
    }

    // SQS can redeliver messages: never overwrite an already provisioned account.
    public async Task CreateIfAbsent(CreateRequest request, string id)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Missing account ID", nameof(id));
        var data = Document.FromJson(request.Data.JsonSerialize());
        var doc = new Document();
        doc.Add("id", id);
        doc.Add("data", data);
        var table = Table.LoadTable(_client, request.Entity);
        var condition = new PutItemOperationConfig
        {
            ConditionalExpression = new Expression { ExpressionStatement = "attribute_not_exists(id)" }
        };
        try
        {
            await table.PutItemAsync(doc, condition);
        }
        catch (Amazon.DynamoDBv2.Model.ConditionalCheckFailedException)
        {
            // A successful earlier delivery is idempotent. Existing data is preserved.
        }
    }

    public async Task<Document> Read(ReadRequest request)
    {
        var table = Table.LoadTable(_client, request.Entity);
        return await table.GetItemAsync(request.Id);
    }

    public async Task Delete(DeleteRequest request)
    {
        var table = Table.LoadTable(_client, request.Entity);
        await table.DeleteItemAsync(request.Id);
    }
}

public class ContextDB : Database
{
    private readonly DynamoDBContext _context;

    public ContextDB()
    {
        _context = new DynamoDBContext(_client);
    }
    public async Task Save<T>(T value)
    {
        await _context.SaveAsync(value);
    }

    public async Task<T> Load<T>(object key)
    {
        return await _context.LoadAsync<T>(key);
    }

    public async Task Delete<T>(object key)
    {
        await _context.DeleteAsync<T>(key);
    }
};
