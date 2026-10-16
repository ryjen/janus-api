#nullable enable

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Janus;

// Candidate v2 schema policy; only use after the consumer field inventory
// and versioned API contract are approved. No v1 caller enables this.
public sealed class RecordFieldSchema
{
    private readonly IReadOnlyDictionary<string, JTokenType> _fields;

    public RecordFieldSchema(IReadOnlyDictionary<string, JTokenType> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        _fields = new Dictionary<string, JTokenType>(fields, StringComparer.Ordinal);
    }

    public bool Matches(string json)
    {
        // JSON has a single string token type. Json.NET's default
        // DateParseHandling.DateTime would silently reinterpret ISO-formatted
        // string values as JTokenType.Date, rejecting a valid string field.
        // Preserve the original JSON token type while enforcing schema types.
        JObject obj;
        try
        {
            using var reader = new JsonTextReader(new StringReader(json))
            {
                DateParseHandling = DateParseHandling.None
            };
            obj = JObject.Load(reader);
            // JObject.Load consumes only the first object. Reject any extra
            // top-level JSON value or comment rather than silently accepting
            // a prefix when the schema is used directly.
            if (reader.Read()) return false;
        }
        catch (JsonException) { return false; }

        foreach (var property in obj.Properties())
        {
            if (!_fields.TryGetValue(property.Name, out var expected) ||
                property.Value.Type != expected)
                return false;
        }
        return true;
    }
}
