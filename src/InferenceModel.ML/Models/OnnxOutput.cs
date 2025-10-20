using Microsoft.ML.Data;

namespace InferenceModel.ML.Models;

/// <summary>
/// ONNX model output tensors
/// </summary>
public class OnnxOutput
{
    [VectorType(2)]
    [ColumnName("logits")]
    public float[] Logits { get; set; } = Array.Empty<float>();
}



