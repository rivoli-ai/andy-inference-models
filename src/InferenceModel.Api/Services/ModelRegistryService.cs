using InferenceModel.Api.Configuration;

namespace InferenceModel.Api.Services;

/// <summary>
/// Service to manage and provide information about available models
/// </summary>
public class ModelRegistryService
{
    private readonly List<ModelInfo> _models;
    private readonly ModelsConfigurationLoader _configLoader;

    public ModelRegistryService(IConfiguration configuration)
    {
        _models = new List<ModelInfo>();
        
        // Get config path from appsettings (or use default)
        var configPath = configuration.GetValue<string>("ModelsConfigPath");
        _configLoader = new ModelsConfigurationLoader(configPath);
        
        try
        {
            // Load models from config/models.json
            var modelsConfig = _configLoader.Load();
            
            foreach (var modelDef in modelsConfig.Models)
            {
                _models.Add(new ModelInfo
                {
                    Id = modelDef.OutputDir, // Use output_dir as ID for consistency
                    Name = modelDef.Name,
                    Description = modelDef.Description ?? $"{modelDef.Name} model",
                    Version = "v1",
                    Labels = modelDef.GetLabels().ToArray(),
                    MaxSequenceLength = modelDef.MaxLength ?? 512,
                    Architecture = modelDef.Id,
                    Status = "available",
                    ModelFolder = modelDef.GetModelFolder(),
                    ModelPath = modelDef.GetModelPath(),
                    TokenizerPath = modelDef.GetTokenizerPath()
                });
            }
        }
        catch (Exception ex)
        {
            // Log error and fall back to default model
            Console.WriteLine($"Warning: Could not load models from config/models.json: {ex.Message}");
            Console.WriteLine("Using default model configuration");
            
            // Default model if config fails to load
            _models.Add(new ModelInfo
            {
                Id = "deberta-v3-base-prompt-injection-v2",
                Name = "DeBERTa v3 Base Prompt Injection v2",
                Description = "DeBERTa v3 base model fine-tuned for prompt injection detection",
                Version = "v2",
                Labels = new[] { "SAFE", "INJECTION" },
                MaxSequenceLength = 512,
                Architecture = "deberta-prompt-injection",
                Status = "available",
                ModelFolder = "models/deberta-v3-base-prompt-injection-v2"
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

