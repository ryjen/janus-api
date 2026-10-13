#nullable enable

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
        JObject obj;
        try { obj = JObject.Parse(json); }
        catch (Newtonsoft.Json.JsonException) { return false; }

        foreach (var property in obj.Properties())
        {
            if (!_fields.TryGetValue(property.Name, out var expected) ||
                property.Value.Type != expected)
                return false;
        }
        return true;
    }
}
