
using Newtonsoft.Json;

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
    [JsonProperty("entity")]
    public string Entity
    {
        get;
        set;
    }

};

public interface KeyRequest
{
    public string Id
    {
        get;
        set;
    }
};

public interface DataRequest
{
    public object Data
    {
        get;
        set;
    }
};

public class ReadRequest : Request, KeyRequest
{
    public string Id
    {
        get;
        set;
    }
};

public class ListRequest : Request { };

public class CreateRequest : Request, DataRequest
{
    [JsonProperty("data")]
    public object Data
    {
        get;
        set;
    }
};

public class UpdateRequest : CreateRequest, KeyRequest
{
    [JsonProperty("id")]
    public string Id
    {
        get;
        set;
    }
};

public class DeleteRequest : ReadRequest
{
}
