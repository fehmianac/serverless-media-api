namespace Domain.Options;

public class UploadSettings
{
    public Dictionary<string, string> AllowedContentTypes { get; set; } = new();
    public Dictionary<string, string> DefaultTags { get; set; } = new();
    public string BucketName { get; set; } = default!;
    public string BaseFolder { get; set; } = default!;
    public double ExpireTime { get; set; }
}