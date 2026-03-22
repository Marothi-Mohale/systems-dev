using System.Text.Json;
using System.Text.Json.Nodes;

namespace EVotingSystem.Infrastructure.Firestore;

public abstract class FirestoreRepositoryBase(IFirestoreDocumentClient client, IFirestoreCollectionNameProvider collectionNames)
{
    protected IFirestoreDocumentClient Client { get; } = client;
    protected IFirestoreCollectionNameProvider Collections { get; } = collectionNames;

    protected static string GetDocumentId(JsonElement document) =>
        document.GetProperty("name").GetString()!.Split('/').Last();

    protected static string GetDocumentName(JsonElement document) =>
        document.GetProperty("name").GetString() ?? string.Empty;

    protected static JsonElement GetFields(JsonElement document) => document.GetProperty("fields");

    protected static string GetString(JsonElement fields, string name) =>
        fields.TryGetProperty(name, out var field) && field.TryGetProperty("stringValue", out var value)
            ? value.GetString() ?? string.Empty
            : string.Empty;

    protected static string? GetNullableString(JsonElement fields, string name) =>
        fields.TryGetProperty(name, out var field) && field.TryGetProperty("stringValue", out var value)
            ? value.GetString()
            : null;

    protected static bool GetBool(JsonElement fields, string name) =>
        fields.TryGetProperty(name, out var field) && field.TryGetProperty("booleanValue", out var value) && value.GetBoolean();

    protected static int GetInt(JsonElement fields, string name) =>
        fields.TryGetProperty(name, out var field) && field.TryGetProperty("integerValue", out var value)
            ? int.Parse(value.GetString() ?? "0")
            : 0;

    protected static DateTime GetDateTime(JsonElement fields, string name) =>
        fields.TryGetProperty(name, out var field) && field.TryGetProperty("timestampValue", out var value)
            ? DateTime.Parse(value.GetString() ?? DateTime.UtcNow.ToString("O"))
            : DateTime.UtcNow;

    protected static DateTime? GetNullableDateTime(JsonElement fields, string name) =>
        fields.TryGetProperty(name, out var field) && field.TryGetProperty("timestampValue", out var value)
            ? DateTime.Parse(value.GetString() ?? DateTime.UtcNow.ToString("O"))
            : null;

    protected static JsonObject BuildDocument(Dictionary<string, object?> values, string? name = null)
    {
        var fields = new JsonObject();
        foreach (var pair in values)
        {
            fields[pair.Key] = ToFirestoreValue(pair.Value);
        }

        var document = new JsonObject
        {
            ["fields"] = fields
        };

        if (!string.IsNullOrWhiteSpace(name))
        {
            document["name"] = name;
        }

        return document;
    }

    protected static JsonObject ToFirestoreValue(object? value)
    {
        return value switch
        {
            null => new JsonObject { ["nullValue"] = null },
            string stringValue => new JsonObject { ["stringValue"] = stringValue },
            bool boolValue => new JsonObject { ["booleanValue"] = boolValue },
            int intValue => new JsonObject { ["integerValue"] = intValue.ToString() },
            long longValue => new JsonObject { ["integerValue"] = longValue.ToString() },
            DateTime dateTimeValue => new JsonObject { ["timestampValue"] = dateTimeValue.ToUniversalTime().ToString("O") },
            _ => new JsonObject { ["stringValue"] = value.ToString() }
        };
    }
}
