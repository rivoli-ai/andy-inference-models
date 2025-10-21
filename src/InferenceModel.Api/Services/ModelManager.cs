using InferenceModel.ML.Models;
using System.Collections.Concurrent;

namespace InferenceModel.Api.Services;

/// <summary>
/// Manages multiple AI models and provides unified access to them
/// </summary>
public class ModelManager : IDisposable
{
    private readonly ConcurrentDictionary<string, PromptGuardServiceWrapper> _models = new();
    private readonly ModelRegistryService _modelRegistry;
    private readonly IConfiguration _configuration;

    public ModelManager(ModelRegistryService modelRegistry, IConfiguration configuration)
    {
        _modelRegistry = modelRegistry;
        _configuration = configuration;
    }

    /// <summary>
    /// Get or load a model instance for the specified model ID
    /// </summary>
    public PromptGuardServiceWrapper GetModel(string modelId)
    {
        // Return existing model if already loaded
        if (_models.TryGetValue(modelId, out var existingModel))
        {
            return existingModel;
        }

        // Load the model
        var model = LoadModel(modelId);
        _models.TryAdd(modelId, model);
        return model;
    }

    /// <summary>
    /// Load a specific model by ID
    /// </summary>
    private PromptGuardServiceWrapper LoadModel(string modelId)
    {
        var modelInfo = _modelRegistry.GetModelById(modelId);
        if (modelInfo == null)
        {
            throw new InvalidOperationException($"Model '{modelId}' not found in registry");
        }

        // Get model folder path
        var modelFolder = modelInfo.ModelFolder ?? throw new InvalidOperationException($"ModelFolder not configured for model '{modelId}'");
        var modelPath = Path.Combine(modelFolder, "model.onnx");
        var tokenizerPath = Path.Combine(modelFolder, "tokenizer.json");

        // Get tokenizer service URL from configuration
        var tokenizerServiceUrl = _configuration.GetValue<string>("ModelConfiguration:TokenizerServiceUrl");

        // Use tokenizer service URL if configured, otherwise use local tokenizer
        var tokenizerPathOrUrl = !string.IsNullOrWhiteSpace(tokenizerServiceUrl)
            ? tokenizerServiceUrl
            : tokenizerPath;

        // Get labels from model info (configured in models.json)
        var labels = modelInfo.Labels;

        return new PromptGuardServiceWrapper(modelPath, tokenizerPathOrUrl, modelId, labels);
    }

    /// <summary>
    /// Check if a model is loaded
    /// </summary>
    public bool IsModelLoaded(string modelId)
    {
        return _models.ContainsKey(modelId) && _models[modelId].IsModelLoaded;
    }

    /// <summary>
    /// Get all loaded model IDs
    /// </summary>
    public IEnumerable<string> GetLoadedModelIds()
    {
        return _models.Keys;
    }

    public void Dispose()
    {
        foreach (var model in _models.Values)
        {
            model.Dispose();
        }
        _models.Clear();
    }
}

