namespace DebertaInferenceModel.Api.Models;

/// <summary>
/// Log entry for a prediction request
/// </summary>
public class PredictionLog
{
    /// <summary>
    /// Unique identifier for the log entry
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// When the prediction was made
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The input text that was analyzed
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// The predicted label (SAFE or INJECTION)
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Confidence score for the prediction
    /// </summary>
    public float Score { get; set; }

    /// <summary>
    /// Whether the text was classified as safe
    /// </summary>
    public bool IsSafe { get; set; }

    /// <summary>
    /// All class scores
    /// </summary>
    public Dictionary<string, float> AllScores { get; set; } = new();

    /// <summary>
    /// How long the prediction took in milliseconds
    /// </summary>
    public double ResponseTimeMs { get; set; }

    /// <summary>
    /// IP address of the requester (optional)
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Whether fallback detection was used
    /// </summary>
    public bool UsedFallback { get; set; }

    /// <summary>
    /// Detection method used (ml-model or keyword-fallback)
    /// </summary>
    public string DetectionMethod { get; set; } = "ml-model";

    /// <summary>
    /// Whether the model was available at the time of prediction
    /// </summary>
    public bool ModelAvailable { get; set; } = true;

    /// <summary>
    /// Model identifier used for detection
    /// </summary>
    public string? Model { get; set; }
}

