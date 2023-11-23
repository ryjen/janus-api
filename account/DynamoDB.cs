using System;
using System.Linq;
using System.Net;
using System.Collections.Generic;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Runtime;
using Newtonsoft.Json;

namespace Janus;

public partial class Account
{
    private static readonly IAmazonDynamoDB _dynamoDbClient = new AmazonDynamoDBClient();
    const string AccountTable = "Account";

    public static async Task<bool> CreateAccount(Object account)
    {
        var doc = Document.FromJson(JsonConvert.SerializeObject(account));

        var request = new PutItemRequest
        {
            TableName = AccountTable,
            Item = doc.ToAttributeMap(),
        };

        var response = await _dynamoDbClient.PutItemAsync(request);
        return response.HttpStatusCode == HttpStatusCode.OK;
    }

    public static async Task<Dictionary<string, object>> GetAccount(Object key)
    {
        var doc = Document.FromJson(JsonConvert.SerializeObject(key));
        var request = new GetItemRequest
        {
            Key = doc.ToAttributeMap(),
                     TableName = AccountTable
        };
        var response = await _dynamoDbClient.GetItemAsync(request);
        return response.Item.ToDictionary(att => att.Key, att => att.Value as object);
    }

    public async Task<bool> UpdateAccount(Object key, Object accoun)
    {
        var id = Document.FromJson(JsonConvert.SerializeObject(key));
        var doc = Document.FromJson(JsonConvert.SerializeObject(account));

        var request = new UpdateItemRequest
        {
            TableName = AccountTable,
            Key = id.ToAttributeMap(),
                    AttributeUpdates = doc.ToAttributeMap()
        };
        var response = await _dynamoDbClient.UpdateItemAsync(request);
        return response.httpStatusCode == HttpStatusCode.OK;
    }

    public static void DeleteAccount(Object key)
    {
        var doc = Document.FromJson(JsonConvert.SerializeObject(key));

        var request = new DeleteItemRequest
        {
            TableName = AccountTable,
            Key = doc.ToAttributeMap(),
        };
        var response = await _dynamoDbClient.DeleteItemAsync(request);
        return response.httpStatusCode == HttpStatusCode.OK;
    }
}
