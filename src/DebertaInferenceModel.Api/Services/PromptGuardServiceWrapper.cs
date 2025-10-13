using DebertaInferenceModel.ML.Models;
using DebertaInferenceModel.ML.Services;

namespace DebertaInferenceModel.Api.Services;

/// <summary>
/// Wrapper service that handles model failures gracefully with fallback detection
/// </summary>
public class PromptGuardServiceWrapper : IDisposable
{
    private readonly PromptGuardService? _mlService;
    private readonly FallbackDetectionService _fallbackService;
    private readonly bool _isUsingFallback;
    private readonly string? _modelError;
    private bool _forceUseModel = true; // Can be toggled via API

    public bool IsModelLoaded => !_isUsingFallback;
    public bool IsUsingFallback => _isUsingFallback;
    public string? ModelError => _modelError;
    public bool ForceUseModel 
    { 
        get => _forceUseModel; 
        set => _forceUseModel = value; 
    }

    public PromptGuardServiceWrapper(string modelPath, string tokenizerPath)
    {
        _fallbackService = new FallbackDetectionService();

        try
        {
            _mlService = new PromptGuardService(modelPath, tokenizerPath);
            _isUsingFallback = false;
            _modelError = null;
        }
        catch (Exception ex)
        {
            _mlService = null;
            _isUsingFallback = true;
            _modelError = ex.Message;
            
            // Log the error (in production, use ILogger)
            Console.WriteLine($"WARNING: Failed to load ML model. Using fallback detection. Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Predict using ML model or fallback to keyword detection
    /// </summary>
    public PromptGuardOutput Predict(string text)
    {
        // If model toggle is off, use fallback
        if (!_forceUseModel)
        {
            return _fallbackService.Predict(text);
        }

        // If model loaded and toggle is on, try to use it
        if (_mlService != null && !_isUsingFallback)
        {
            try
            {
                return _mlService.Predict(text);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WARNING: ML prediction failed, using fallback. Error: {ex.Message}");
                return _fallbackService.Predict(text);
            }
        }

        // Model not loaded, use fallback
        return _fallbackService.Predict(text);
    }

    /// <summary>
    /// Get current detection method being used
    /// </summary>
    public string GetCurrentDetectionMethod()
    {
        if (!_forceUseModel)
        {
            return "keyword-fallback (forced)";
        }
        
        return (_mlService != null && !_isUsingFallback) ? "ml-model" : "keyword-fallback";
    }

    /// <summary>
    /// Check if currently using fallback (either forced or due to model failure)
    /// </summary>
    public bool IsCurrentlyUsingFallback()
    {
        return !_forceUseModel || _isUsingFallback || _mlService == null;
    }

    public void Dispose()
    {
        _mlService?.Dispose();
    }
}

