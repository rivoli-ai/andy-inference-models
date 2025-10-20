using System.Text.Json;
using System.Text.Json.Serialization;

namespace InferenceModel.Api.Configuration;

#region Legacy Configuration (Backward Compatibility)

/// <summary>
/// Legacy configuration for model and tokenizer paths
/// Kept for backward compatibility - prefer using ModelsConfigurationLoader
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

    /// <summary>
    /// URL of the Python tokenizer service (optional, uses TokenizerPath if not provided)
    /// When provided, uses Python service for accurate HuggingFace tokenization
    /// </summary>
    public string? TokenizerServiceUrl { get; set; }
}

#endregion

#region Models Configuration from config/models.json

/// <summary>
/// Root configuration loaded from config/models.json
/// This is the primary configuration source for all models
/// </summary>
public class ModelsConfiguration
{
    [JsonPropertyName("models")]
    public List<ModelDefinition> Models { get; set; } = new();

    [JsonPropertyName("config")]
    public GlobalConfig Config { get; set; } = new();
}

/// <summary>
/// Individual model definition from config/models.json
/// </summary>
public class ModelDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("huggingface_model")]
    public string? HuggingFaceModel { get; set; }

    [JsonPropertyName("output_dir")]
    public string OutputDir { get; set; } = string.Empty;

    [JsonPropertyName("check_files")]
    public List<string> CheckFiles { get; set; } = new();

    [JsonPropertyName("task_type")]
    public string? TaskType { get; set; }

    [JsonPropertyName("max_length")]
    public int? MaxLength { get; set; }

    [JsonPropertyName("opset_version")]
    public int? OpsetVersion { get; set; }

    [JsonPropertyName("required")]
    public bool? Required { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Get the full path to the model directory
    /// </summary>
    public string GetModelFolder(string baseDir = "models") => 
        Path.Combine(baseDir, OutputDir);

    /// <summary>
    /// Get the full path to the ONNX model file
    /// </summary>
    public string GetModelPath(string baseDir = "models") => 
        Path.Combine(GetModelFolder(baseDir), "model.onnx");

    /// <summary>
    /// Get the full path to the tokenizer file
    /// </summary>
    public string GetTokenizerPath(string baseDir = "models") => 
        Path.Combine(GetModelFolder(baseDir), "tokenizer.json");

    /// <summary>
    /// Get labels for classification (derived from model ID)
    /// </summary>
    public List<string> GetLabels()
    {
        // Use model-specific labels based on ID
        // This could be enhanced to read from model config.json
        return Id switch
        {
            "deberta-prompt-injection" => new List<string> { "SAFE", "INJECTION" },
            "graphcodebert-vulnerability" => new List<string> { "SAFE", "VULNERABLE" },
            _ => new List<string> { "NEGATIVE", "POSITIVE" }
        };
    }
}

/// <summary>
/// Global configuration from config/models.json
/// </summary>
public class GlobalConfig
{
    [JsonPropertyName("models_base_dir")]
    public string? ModelsBaseDir { get; set; }

    [JsonPropertyName("conversion_script")]
    public string? ConversionScript { get; set; }

    [JsonPropertyName("python_requirements")]
    public List<string>? PythonRequirements { get; set; }

    [JsonPropertyName("tokenizer_service_url")]
    public string? TokenizerServiceUrl { get; set; }
}

#endregion

#region Configuration Loader

/// <summary>
/// Service to load and provide access to models configuration from config/models.json
/// This is the primary way to access model configurations
/// </summary>
public class ModelsConfigurationLoader
{
    private readonly string _configPath;
    private ModelsConfiguration? _configuration;

    public ModelsConfigurationLoader(string? configPath = null)
    {
        _configPath = configPath ?? Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "config", "models.json"
        );
    }

    /// <summary>
    /// Load configuration from config/models.json
    /// </summary>
    public ModelsConfiguration Load()
    {
        if (_configuration != null)
            return _configuration;

        var configFile = FindConfigFile();
        
        if (!File.Exists(configFile))
        {
            throw new FileNotFoundException(
                $"Configuration file not found. Searched: {configFile}. " +
                "Make sure config/models.json exists in the project root."
            );
        }

        var json = File.ReadAllText(configFile);
        _configuration = JsonSerializer.Deserialize<ModelsConfiguration>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        });

        if (_configuration == null)
        {
            throw new InvalidOperationException("Failed to load models configuration");
        }

        return _configuration;
    }

    /// <summary>
    /// Find the config/models.json file, searching multiple possible locations
    /// </summary>
    private string FindConfigFile()
    {
        // Try multiple possible locations
        var possiblePaths = new[]
        {
            _configPath,
            Path.Combine(AppContext.BaseDirectory, "config", "models.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "config", "models.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "config", "models.json"),
            "config/models.json",
            "../config/models.json",
            "../../config/models.json"
        };

        foreach (var path in possiblePaths)
        {
            try
            {
                var fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
            catch
            {
                // Ignore path resolution errors and try next
                continue;
            }
        }

        // Return the preferred path for error message
        return Path.GetFullPath(_configPath);
    }

    /// <summary>
    /// Get a specific model by ID
    /// </summary>
    public ModelDefinition? GetModel(string id)
    {
        var config = Load();
        return config.Models.FirstOrDefault(m => 
            m.Id.Equals(id, StringComparison.OrdinalIgnoreCase) ||
            m.OutputDir.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Get all available models
    /// </summary>
    public List<ModelDefinition> GetAllModels()
    {
        var config = Load();
        return config.Models;
    }

    /// <summary>
    /// Get global configuration
    /// </summary>
    public GlobalConfig GetGlobalConfig()
    {
        var config = Load();
        return config.Config;
    }
}

#endregion

