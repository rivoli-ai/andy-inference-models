namespace DebertaInferenceModel.Api.Configuration;

/// <summary>
/// Configuration for model and tokenizer paths
/// </summary>
public class ModelConfiguration
{
    public const string SectionName = "ModelConfiguration";

    /// <summary>
    /// Path to the ONNX model file
    /// </summary>
    public string ModelPath { get; set; } = "models/model.onnx";

    /// <summary>
    /// Path to the tokenizer JSON file
    /// </summary>
    public string TokenizerPath { get; set; } = "models/tokenizer.json";
}

