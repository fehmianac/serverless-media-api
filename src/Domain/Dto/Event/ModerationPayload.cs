namespace Domain.Dto.Event;

public class ModerationPayload
{
    public string Key { get; set; } = default!;
    public string Bucket { get; set; } = default!;
    public Dictionary<string, string> Tags { get; set; } = new();
}