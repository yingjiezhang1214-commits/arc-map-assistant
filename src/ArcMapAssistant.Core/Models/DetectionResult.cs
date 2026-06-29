namespace ArcMapAssistant.Core.Models;

public sealed class DetectionResult
{
    public bool Success { get; set; }
    public bool Matched { get; set; }
    public string MapId { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public double Threshold { get; set; }
    public long ElapsedMs { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
