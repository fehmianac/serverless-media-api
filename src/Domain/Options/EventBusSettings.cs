namespace Domain.Options;

public class EventBusSettings
{
    public bool IsEnabled { get; set; }
    public string TopicArn { get; set; } = default!;
}