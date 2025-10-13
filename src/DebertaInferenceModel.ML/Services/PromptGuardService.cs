using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using DebertaInferenceModel.ML.Models;

namespace DebertaInferenceModel.ML.Services;

/// <summary>
/// Service for detecting prompt injection using DeBERTa v3 model
/// </summary>
public class PromptGuardService : IDisposable
{
    private readonly InferenceSession _session;
    private readonly DebertaTokenizer? _tokenizer;
    private readonly string[] _labels = new[] { "SAFE", "INJECTION" };

    public PromptGuardService(string modelPath, string tokenizerPathOrServiceUrl)
    {
        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException($"Model file not found at: {modelPath}");
        }

        // Check if tokenizerPathOrServiceUrl is a URL or file path
        bool isUrl = tokenizerPathOrServiceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                     tokenizerPathOrServiceUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        // Only check file existence if it's not a URL
        if (!isUrl && !File.Exists(tokenizerPathOrServiceUrl))
        {
            throw new FileNotFoundException($"Tokenizer file not found at: {tokenizerPathOrServiceUrl}");
        }

        // Initialize tokenizer (handles both URLs and file paths)
        _tokenizer = new DebertaTokenizer(tokenizerPathOrServiceUrl);

        // Load ONNX model using ONNX Runtime directly
        _session = new InferenceSession(modelPath);
    }

    /// <summary>
    /// Predict if the given text contains prompt injection
    /// </summary>
    public PromptGuardOutput Predict(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new PromptGuardOutput
            {
                Label = "SAFE",
                Score = 1.0f,
                AllScores = new Dictionary<string, float>
                {
                    { "SAFE", 1.0f },
                    { "INJECTION", 0.0f }
                }
            };
        }

        if (_tokenizer == null || _session == null)
        {
            throw new InvalidOperationException("Model not properly initialized");
        }

        // Tokenize input
        var (inputIds, attentionMask) = _tokenizer.Encode(text);

        // Create input tensors with proper shape [1, 512] (batch_size=1, seq_length=512)
        var inputIdsTensor = new DenseTensor<long>(inputIds, new[] { 1, 512 });
        var attentionMaskTensor = new DenseTensor<long>(attentionMask, new[] { 1, 512 });

        // Create input container
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids", inputIdsTensor),
            NamedOnnxValue.CreateFromTensor("attention_mask", attentionMaskTensor)
        };

        // Run inference
        using var results = _session.Run(inputs);
        
        // Get logits from output
        var logitsTensor = results.First().AsEnumerable<float>().ToArray();
        
        // Apply softmax to logits
        var scores = Softmax(logitsTensor);

        // Get predicted label
        var maxIndex = scores.ToList().IndexOf(scores.Max());
        var predictedLabel = _labels[maxIndex];

        return new PromptGuardOutput
        {
            Label = predictedLabel,
            Score = scores[maxIndex],
            AllScores = new Dictionary<string, float>
            {
                { _labels[0], scores[0] },
                { _labels[1], scores[1] }
            }
        };
    }

    /// <summary>
    /// Apply softmax function to convert logits to probabilities
    /// </summary>
    private float[] Softmax(float[] logits)
    {
        var maxLogit = logits.Max();
        var exp = logits.Select(l => Math.Exp(l - maxLogit)).ToArray();
        var sumExp = exp.Sum();
        return exp.Select(e => (float)(e / sumExp)).ToArray();
    }

    public void Dispose()
    {
        _session?.Dispose();
    }
}

