namespace DebertaInferenceModel.Api.Models;

/// <summary>
/// Extended response model with system metadata
/// </summary>
public class DetectResponseWithMetadata : DetectResponse
{
    /// <summary>
    /// Whether fallback detection was used
    /// </summary>
    public bool UsingFallback { get; set; }

    /// <summary>
    /// Detection method used
    /// </summary>
    public string DetectionMethod { get; set; } = "ml-model";
}



