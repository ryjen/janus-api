using Amazon.Lambda.APIGatewayEvents;
using static System.Net.HttpStatusCode;

namespace Janus;

using static Shared;

// All exposed record IDs are derived from the validated Cognito authorizer.
// Client-supplied IDs may only confirm the same identity, never select another record.
public abstract class ModelHandler : DataHandler
{
    private readonly string _entity;
    private readonly bool _readOnly;

    protected ModelHandler(string entity, bool readOnly = false)
    {
        _readOnly = readOnly;
        _entity = Environment.GetEnvironmentVariable(entity.ToUpperInvariant() + "_TABLE")
            ?? throw new InvalidOperationException("Required table configuration is missing");
    }

    public override async Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var claims = request.RequestContext?.Authorizer?.Claims;
        if (claims == null || !claims.TryGetValue("sub", out var subject) || string.IsNullOrWhiteSpace(subject))
            return Response(Unauthorized, new { Message = "Authentication required" });

        if (_readOnly && request.HttpMethod != "GET")
            return Response(MethodNotAllowed, new { Message = "This record is read-only" });

        try
        {
            switch (request.HttpMethod)
            {
                case "GET":
                    if (!AccessPolicy.MatchesSubject(subject, request.QueryStringParameters is { } query && query.TryGetValue("id", out var queryId) ? queryId : null))
                        return Response(Forbidden, new { Message = "Access denied" });
                    return await Read(new ReadRequest { Entity = _entity, Id = subject });
                case "POST":
                case "PUT":
                    var update = request.Body.ToParams();
                    if (!AccessPolicy.MatchesSubject(subject, update.GetValueOrDefault("id")?.ToString()))
                        return Response(Forbidden, new { Message = "Access denied" });
                    update.Remove("id");
                    return await Update(new UpdateRequest { Entity = _entity, Id = subject, Data = update });
                case "DELETE":
                    var parameters = request.Body.ToParams();
                    if (!AccessPolicy.MatchesSubject(subject, parameters.GetValueOrDefault("id")?.ToString()))
                        return Response(Forbidden, new { Message = "Access denied" });
                    return await Delete(new DeleteRequest { Entity = _entity, Id = subject });
                default:
                    return Response(BadRequest, new { Message = "Invalid request" });
            }
        }
        catch
        {
            return Response(BadRequest, new { Message = "Invalid request" });
        }
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
            if (entity == null) return Response(NotFound, new { Message = "Record not found" });
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
