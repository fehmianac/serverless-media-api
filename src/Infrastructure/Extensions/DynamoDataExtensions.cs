using System.Text.Json;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Domain.Entities.Base;

namespace Infrastructure.Extensions;

public static class DynamoDataExtensions
{
    public static Dictionary<string, AttributeValue> ToAttributeMap(this IEntity entity)
    {
        var json = JsonSerializer.Serialize<object>(entity);
        var document = Document.FromJson(json);
        return document.ToAttributeMap();
    }

    public static Dictionary<string, AttributeValue> ToKeyAttributeMap(this IEntity entity)
    {
        var json = JsonSerializer.Serialize(entity);
        var document = Document.FromJson(json);
        return document.ToAttributeMap();
    }

    public static T ToEntity<T>(this Dictionary<string, AttributeValue> attributeMap)
    {
        var document = Document.FromAttributeMap(attributeMap);
        var json = document.ToJson();
        return JsonSerializer.Deserialize<T>(json)!;
    }
}