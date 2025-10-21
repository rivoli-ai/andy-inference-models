using Microsoft.ML.Data;

namespace InferenceModel.ML.Models;

/// <summary>
/// ONNX model input tensors
/// </summary>
public class OnnxInput
{
    [VectorType(512)]
    [ColumnName("input_ids")]
    public long[] InputIds { get; set; } = Array.Empty<long>();

    [VectorType(512)]
    [ColumnName("attention_mask")]
    public long[] AttentionMask { get; set; } = Array.Empty<long>();
}

