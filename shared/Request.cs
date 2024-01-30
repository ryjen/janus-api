
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
    public object Data
    {
        get;
        set;
    }
};

public class UpdateRequest : CreateRequest, KeyRequest
{
    public string Id
    {
        get;
        set;
    }
};

public class DeleteRequest : ReadRequest
{
}
