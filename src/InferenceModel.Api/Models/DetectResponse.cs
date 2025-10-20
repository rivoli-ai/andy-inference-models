namespace InferenceModel.Api.Models;

/// <summary>
/// Response model for prompt injection detection
/// </summary>
public class DetectResponse
{
    /// <summary>
    /// The predicted label (SAFE or INJECTION)
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// The confidence score for the predicted label
    /// </summary>
    public float Score { get; set; }

    /// <summary>
    /// All class scores
    /// </summary>
    public Dictionary<string, float> Scores { get; set; } = new();

    /// <summary>
    /// Whether the text is considered safe (not a prompt injection)
    /// </summary>
    public bool IsSafe { get; set; }

    /// <summary>
    /// The analyzed text
    /// </summary>
    public string Text { get; set; } = string.Empty;
}



