
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2;

namespace Janus;

public class Database
{
    private readonly DynamoDBContext _context = new DynamoDBContext(new AmazonDynamoDBClient());

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
}
