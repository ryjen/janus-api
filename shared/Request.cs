
using Amazon.Lambda.APIGatewayEvents;

namespace Janus;

public interface EntityRequest
{

    public string Entity
    {
        get;
        set;
    }
}

public abstract class Request : EntityRequest
{
    public string Entity
    {
        get;
        set;
    }
};

public interface KeyRequest
{
    public string Key
    {
        get;
        set;
    }
};

public interface DataRequest
{
    public string Data
    {
        get;
        set;
    }
};

public class ReadRequest : Request, KeyRequest
{
    public string Key
    {
        get;
        set;
    }
};

public class ListRequest : Request { };

public class CreateRequest : Request, DataRequest
{
    public string Data
    {
        get;
        set;
    }
};

public class UpdateRequest : CreateRequest, KeyRequest
{
    public string Key
    {
        get;
        set;
    }
};

public class DeleteRequest : ReadRequest
{
    public string Data = null;
}

public static partial class Convert
{

public static T ParseJson<T>(this APIGatewayProxyRequest request) where T :
    Request
    {
        return request.Body.JsonDeserialize<T>();
    }

public static T ParseForm<T>(this APIGatewayProxyRequest request) where T :
    Request
    {
        var json = request.QueryStringParameters.JsonSerialize();
        return json.JsonDeserialize<T>();
    }
};
