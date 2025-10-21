namespace InferenceModel.Api.Models;

/// <summary>
/// Extended response model with system metadata
/// </summary>
public class PredictResponseWithMetadata : PredictResponse
{
    /// <summary>
    /// Whether fallback prediction was used
    /// </summary>
    public bool UsingFallback { get; set; }

    /// <summary>
    /// Prediction method used
    /// </summary>
    public string PredictionMethod { get; set; } = "ml-model";

    /// <summary>
    /// Model identifier used for prediction
    /// </summary>
    public string? Model { get; set; }
}

