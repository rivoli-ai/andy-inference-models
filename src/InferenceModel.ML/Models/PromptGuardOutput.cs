namespace InferenceModel.ML.Models;

/// <summary>
/// Output result from prompt injection detection
/// </summary>
public class PromptGuardOutput
{
    public string Label { get; set; } = string.Empty;
    public float Score { get; set; }
    public Dictionary<string, float> AllScores { get; set; } = new();
}



