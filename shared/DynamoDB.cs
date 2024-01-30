
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
        doc.Add("Data", data);
        var table = Table.LoadTable(_client, request.Entity);
        await table.UpdateItemAsync(doc, request.Id);
    }

    public async Task<string> Create(CreateRequest request)
    {
        var data = Document.FromJson(request.Data.JsonSerialize());
        var doc = new Document();
        var id = Guid.NewGuid().ToString();
        doc.Add("Id", id);
        doc.Add("Data", data);
        var table = Table.LoadTable(_client, request.Entity);
        await table.PutItemAsync(doc);
        return id;
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
