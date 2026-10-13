#nullable enable

using Amazon.Lambda.APIGatewayEvents;
using static System.Net.HttpStatusCode;

namespace Janus;

using static Shared;

// Lambda adapter: translates HTTP into application operations. Authorization
// lives in SubjectRecordService, persistence behind IRecordStore.
public abstract class ModelHandler
{
    private readonly SubjectRecordService _records;
    private readonly string _entity;

    protected ModelHandler(string entity, bool readOnly = false, IRecordStore? store = null)
    {
        _entity = entity;
        _records = new SubjectRecordService(store ?? new DynamoRecordStore(entity), readOnly);
    }

    public async Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var claims = request.RequestContext?.Authorizer?.Claims;
        string? subject = null;
        if (claims is not null) claims.TryGetValue("sub", out subject);

        if (string.IsNullOrWhiteSpace(subject))
            return Response(Unauthorized, new { Message = "Authentication required" });

        if (_records.IsReadOnly && request.HttpMethod != "GET")
            return Response(MethodNotAllowed, new { Message = "This record is read-only" });

        try
        {
            RecordResult result;
            switch (request.HttpMethod)
            {
                case "GET":
                    string? id = null;
                    request.QueryStringParameters?.TryGetValue("id", out id);
                    result = await _records.ReadAsync(subject, id);
                    break;
                case "POST":
                case "PUT":
                    if (!RecordRequestParser.TryParseMutation(request.Body, out var updateRequest))
                        return Response(BadRequest, new { Message = "Invalid JSON object" });
                    result = await _records.UpsertAsync(subject, updateRequest!);
                    break;
                case "DELETE":
                    if (!RecordRequestParser.TryParseMutation(request.Body, out var deleteRequest))
                        return Response(BadRequest, new { Message = "Invalid JSON object" });
                    result = await _records.DeleteAsync(subject, deleteRequest!);
                    break;
                default:
                    return Response(MethodNotAllowed, new { Message = "Method not allowed" });
            }

            return result.Status switch
            {
                RecordStatus.Unauthorized => Response(Unauthorized, new { Message = "Authentication required" }),
                RecordStatus.Forbidden => Response(Forbidden, new { Message = "Access denied" }),
                RecordStatus.ReadOnly => Response(MethodNotAllowed, new { Message = "This record is read-only" }),
                RecordStatus.Missing => Response(NotFound, new { Message = "Record not found" }),
                RecordStatus.Ok when request.HttpMethod == "GET" => Response(OK, result.Json!),
                RecordStatus.Ok when request.HttpMethod == "DELETE" => Response(OK, new { Message = _entity + " deleted" }),
                RecordStatus.Ok => Response(OK, new { Message = _entity + " updated" }),
                _ => Response(InternalServerError, new { Message = "Unexpected record status" })
            };
        }
        catch (Exception)
        {
            // Do not turn AWS/DynamoDB outages into a caller 400. No secrets or
            // raw payloads are logged; monitoring can count 503s at the gateway.
            return Response(ServiceUnavailable, new { Message = "Record service unavailable" });
        }
    }

}
