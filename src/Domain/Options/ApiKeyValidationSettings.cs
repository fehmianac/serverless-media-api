namespace Domain.Options;

public class ApiKeyValidationSettings
{
    public bool IsEnabled { get; set; }
    public string? ApiKey { get; set; }

    public string? HeaderName { get; set; }
    public List<string> WhiteList { get; set; } = new();
}