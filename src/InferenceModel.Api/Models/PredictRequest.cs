using System.ComponentModel.DataAnnotations;

namespace InferenceModel.Api.Models;

/// <summary>
/// Request model for prediction
/// </summary>
public class PredictRequest
{
    /// <summary>
    /// The text to analyze
    /// </summary>
    [Required]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// The model to use for prediction
    /// NOTE: Only required for batch predictions (/api/predict/batch).
    /// For single predictions (/api/models/{model}/predict), the model is specified in the route parameter and this field is ignored.
    /// </summary>
    public string? Model { get; set; }
}

