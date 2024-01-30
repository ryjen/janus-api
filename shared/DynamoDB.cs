
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
        await table.UpdateItemAsync(doc, request.Key);
    }

    public async Task Create(CreateRequest request)
    {
        var data = Document.FromJson(request.Data.JsonSerialize());
        var doc = new Document();

        doc.Add("Id", Guid.NewGuid());
        doc.Add("Data", data);
        var table = Table.LoadTable(_client, request.Entity);
        await table.PutItemAsync(doc);
    }

    public async Task<Document> Read(ReadRequest request)
    {
        var table = Table.LoadTable(_client, request.Entity);
        return await table.GetItemAsync(request.Key);
    }

    public async Task Delete(DeleteRequest request)
    {
        var table = Table.LoadTable(_client, request.Entity);

        if (request.Data == null)
        {
            await table.DeleteItemAsync(request.Key);
            return;
        }

        var data = Document.FromJson(request.Data.JsonSerialize());
        var doc = new Document();

        doc.Add("Id", request.Key);
        doc.Add("Data", data);
        await table.DeleteItemAsync(doc);
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
