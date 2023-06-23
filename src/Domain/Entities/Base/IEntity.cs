using System.Text.Json.Serialization;

namespace Domain.Entities.Base;

public interface IEntity
{
    [JsonPropertyName("pk")] public string Pk { get; }
    [JsonPropertyName("sk")] public string Sk { get; }
}