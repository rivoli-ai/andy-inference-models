using Microsoft.ML.Data;

namespace DebertaInferenceModel.ML.Models;

/// <summary>
/// ONNX model output tensors
/// </summary>
public class OnnxOutput
{
    [VectorType(2)]
    [ColumnName("logits")]
    public float[] Logits { get; set; } = Array.Empty<float>();
}



