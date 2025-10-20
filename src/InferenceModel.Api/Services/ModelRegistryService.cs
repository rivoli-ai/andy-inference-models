using Microsoft.Extensions.Options;

namespace InferenceModel.Api.Services;

/// <summary>
/// Service to manage and provide information about available models
/// </summary>
public class ModelRegistryService
{
    private readonly List<ModelInfo> _models;

    public ModelRegistryService(IConfiguration configuration)
    {
        _models = new List<ModelInfo>();
        
        // Load models from configuration
        var modelsSection = configuration.GetSection("AvailableModels");
        var configuredModels = modelsSection.Get<List<ModelInfo>>();
        
        if (configuredModels != null && configuredModels.Any())
        {
            _models.AddRange(configuredModels);
        }
        else
        {
            // Default model if none configured
            _models.Add(new ModelInfo
            {
                Id = "deberta-v3-base-prompt-injection-v2",
                Name = "DeBERTa v3 Base Prompt Injection v2",
                Description = "DeBERTa v3 base model fine-tuned for prompt injection detection",
                Version = "v2",
                Labels = new[] { "SAFE", "INJECTION" },
                MaxSequenceLength = 512,
                Architecture = "deberta-v3-base",
                Status = "available"
            });
        }
    }

    /// <summary>
    /// Get all available models
    /// </summary>
    public IReadOnlyList<ModelInfo> GetAllModels() => _models.AsReadOnly();

    /// <summary>
    /// Get model by ID
    /// </summary>
    public ModelInfo? GetModelById(string modelId)
    {
        return _models.FirstOrDefault(m => 
            m.Id.Equals(modelId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Check if model exists
    /// </summary>
    public bool ModelExists(string modelId)
    {
        return _models.Any(m => 
            m.Id.Equals(modelId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Get list of model IDs
    /// </summary>
    public IReadOnlyList<string> GetModelIds()
    {
        return _models.Select(m => m.Id).ToList().AsReadOnly();
    }
}

/// <summary>
/// Model information
/// </summary>
public class ModelInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string[] Labels { get; set; } = Array.Empty<string>();
    public int MaxSequenceLength { get; set; }
    public string Architecture { get; set; } = string.Empty;
    public string Status { get; set; } = "available";
    
    /// <summary>
    /// Path to the model folder (optional, defaults to models/{Id})
    /// </summary>
    public string? ModelFolder { get; set; }
    
    /// <summary>
    /// Path to ONNX model file (optional, defaults to {ModelFolder}/model.onnx)
    /// </summary>
    public string? ModelPath { get; set; }
    
    /// <summary>
    /// Path to tokenizer file (optional, defaults to {ModelFolder}/tokenizer.json)
    /// </summary>
    public string? TokenizerPath { get; set; }
    
    /// <summary>
    /// Get the resolved model folder path
    /// </summary>
    public string GetModelFolder() => ModelFolder ?? $"models/{Id}";
    
    /// <summary>
    /// Get the resolved model file path
    /// </summary>
    public string GetModelPath() => ModelPath ?? $"{GetModelFolder()}/model.onnx";
    
    /// <summary>
    /// Get the resolved tokenizer file path
    /// </summary>
    public string GetTokenizerPath() => TokenizerPath ?? $"{GetModelFolder()}/tokenizer.json";
}

