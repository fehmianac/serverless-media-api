namespace LabelChecker.Options;

public class ImageModerationConfig
{
    public bool IsEnabled { get; set; }
    public List<string> ForbiddenLabels { get; set; } = new();
    public string TopicArn { get; set; } = default!;
    public float AlertConfidence { get; set; } = 90;
    public float MinConfidence { get; set; } = 60;
}