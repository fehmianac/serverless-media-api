using System.Text.Json.Serialization;
using Domain.Entities.Base;

namespace Domain.Entities;

public class GalleryEntity : IEntity
{
    [JsonPropertyName("pk")] public string Pk => $"galleries#{UserId}";
    [JsonPropertyName("sk")] public string Sk => $"{ItemId}";

    [JsonPropertyName("userId")] public string UserId { get; set; } = default!;
    [JsonPropertyName("uniqueKey")] public string ItemId { get; set; } = default!;
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("images")] public List<GalleryImageModel> Images { get; set; } = new();
    [JsonPropertyName("createdAt")] public DateTime CreatedAt { get; set; }
    [JsonPropertyName("updatedAt")] public DateTime UpdatedAt { get; set; }

    public class GalleryImageModel
    {
        [JsonPropertyName("id")] public string Id { get; set; } = default!;
        [JsonPropertyName("url")] public string Url { get; set; } = default!;
        [JsonPropertyName("rank")] public int Rank { get; set; }
    }
}