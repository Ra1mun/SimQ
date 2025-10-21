using System.Collections;
using System.Text.Json;
using MongoDB.Bson;

namespace SimQ.Core.Extensions;

public static class BsonValueExtensions
{
    public static object? ToDotNetValue(this BsonValue value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        switch (value.BsonType)
        {
            case BsonType.Decimal128:
                return value.AsDecimal128;
            case BsonType.Double:
                return value.AsDouble;
            case BsonType.Int32:
                return value.AsInt32;
            case BsonType.Int64:
                return value.AsInt64;
            case BsonType.Null:
                return null;
            default:
                throw new NotSupportedException($"BsonType {value.BsonType} is not supported.");
        }
    }

    public static IEnumerable<BsonValue> AsBsonValue(this IEnumerable<JsonElement> elements)
    {
        return elements.Select(ToBsonValue);
    }
    
    public static BsonValue ToBsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt32(out var i) => i,
            JsonValueKind.Number when element.TryGetInt64(out var l) => l,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => BsonNull.Value,
            JsonValueKind.Object => BsonDocument.Parse(element.GetRawText()),
            JsonValueKind.Array => new BsonArray(element.EnumerateArray().Select(ToBsonValue)),
            _ => throw new NotSupportedException($"Unsupported JSON value kind: {element.ValueKind}")
        };
    }
}