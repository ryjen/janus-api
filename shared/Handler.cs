#nullable enable

using Amazon.Lambda.APIGatewayEvents;
using Newtonsoft.Json;
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
                    if (!TryParseFields(request.Body, out var updateFields))
                        return Response(BadRequest, new { Message = "Invalid JSON object" });
                    result = await _records.UpsertAsync(subject, updateFields!);
                    break;
                case "DELETE":
                    if (!TryParseFields(request.Body, out var deleteFields))
                        return Response(BadRequest, new { Message = "Invalid JSON object" });
                    result = await _records.DeleteAsync(subject, deleteFields!);
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

    // Restrict client-error translation to parsing only. An ArgumentException or
    // JsonException thrown by storage must not be mistaken for a bad request.
    private static bool TryParseFields(string? body, out Dictionary<string, object>? fields)
    {
        fields = null;
        if (string.IsNullOrWhiteSpace(body))
            return false;

        try
        {
            fields = body.ToParams();
            return fields is not null;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
