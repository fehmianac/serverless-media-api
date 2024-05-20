using System.Text.Json.Serialization;
using Domain.Entities.Base;

namespace Domain.Entities;

public class ImageGalleryMappingEntity : IEntity
{
    [JsonPropertyName("pk")]   public string Pk => GetPk();
    
    [JsonPropertyName("sk")]public string Sk => ImageUrl;
    [JsonPropertyName("imageUrl")]public string ImageUrl { get; set; } = default!;
    [JsonPropertyName("userId")]public string UserId { get; set; } = default!;
    [JsonPropertyName("itemId")]public string ItemId { get; set; } = default!;
    [JsonPropertyName("createdAt")]public DateTime CreatedAt { get; set; } = default!;
    
    public static string GetPk()=>$"imageGalleryMapping";
}