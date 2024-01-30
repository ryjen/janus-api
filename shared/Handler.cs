
using Amazon.Lambda.APIGatewayEvents;
using static System.Net.HttpStatusCode;

namespace Janus;

using static Shared;

public abstract class ModelHandler : DataHandler
{
    private readonly string _entity;
    public ModelHandler(string entity)
    {
        _entity = entity;
    }

    public override async Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            switch (request.HttpMethod)
            {
            case "GET":
                return await Read(ReadRequest(request));
            case "POST":
            case "PUT":
                return await Update(UpdateRequest(request));
            case "DELETE":
                return await Delete(DeleteRequest(request));
            }

            return Response(BadRequest, new { Message = "Invalid request" });
        }
        catch
        {
            return Response(InternalServerError, new { Message = "Internal error" });
        }
    }

    private ReadRequest ReadRequest(APIGatewayProxyRequest request)
    {
        var param = request.ToParams();
        var id = param["id"].ToString();

        return new ReadRequest
        {
            Entity = _entity,
            Id = id
        };
    }
    private UpdateRequest UpdateRequest(APIGatewayProxyRequest request)
    {
        var param = request.ToParams();
        var id = param.Remove("id");
        return new UpdateRequest
        {
            Entity = _entity,
            Id = id.ToString(),
                   Data = param,
        };
    }

    private DeleteRequest DeleteRequest(APIGatewayProxyRequest request)
    {
        var param = request.ToParams();
        var id = param["id"].ToString();
        return new DeleteRequest
        {
            Entity = _entity,
            Id = id
        };
    }

}

public abstract class DataHandler
{
    private readonly Database _db;

    public DataHandler()
    {
        _db = new Database();
    }

    public abstract Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context);

    public async Task<APIGatewayProxyResponse> Create(CreateRequest request)
    {
        try
        {
            var id = await _db.Create(request);
            return Response(OK, new { Id = id, Message = request.Entity + " created" });
        }
        catch
        {
            return Response(BadRequest, new { Message = request.Entity + " create failed" });
        }
    }
    public async Task<APIGatewayProxyResponse> Read(ReadRequest request)
    {
        try
        {
            var entity = await _db.Read(request);
            return Response(OK, entity.ToJson());
        }
        catch
        {
            return Response(BadRequest, new { Message = request.Entity + " read failed" });
        }
    }

    public async Task<APIGatewayProxyResponse> Update(UpdateRequest request)
    {
        try
        {
            await _db.Update(request);
            return Response(OK, new { Message = string.Format("{0} updated", request.Entity) });
        }
        catch
        {
            return Response(BadRequest, new { Message = string.Format("{0} update failed") });
        }
    }
    public async Task<APIGatewayProxyResponse> Delete(DeleteRequest request)
    {
        try
        {
            await _db.Delete(request);
            return Response(OK, new { Message = string.Format("{0} deleted", request.Entity) });
        }
        catch
        {
            return Response(BadRequest, new { Message = string.Format("{0} delete failed", request.Entity) });
        }
    }

}
