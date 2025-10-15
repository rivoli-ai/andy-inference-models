using System.ComponentModel.DataAnnotations;

namespace DebertaInferenceModel.Api.Models;

/// <summary>
/// Request model for prompt injection detection
/// </summary>
public class DetectRequest
{
    /// <summary>
    /// The text to analyze for prompt injection
    /// </summary>
    [Required]
    public string Text { get; set; } = string.Empty;
}



