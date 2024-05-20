using System.Text.Json.Serialization;
using Domain.Entities.Base;

namespace Domain.Entities;

public class ProblematicImagesEntity : IEntity
{
    [JsonPropertyName("pk")] public string Pk => GetPk();

    [JsonPropertyName("sk")] public string Sk => ImageUrl;
    [JsonPropertyName("imageUrl")] public string ImageUrl { get; set; } = default!;

    [JsonPropertyName("createdAt")] public DateTime CreatedAt { get; set; } = default!;
    private string GetPk() => $"problematicImages";
}