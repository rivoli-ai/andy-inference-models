using DebertaInferenceModel.ML.Models;

namespace DebertaInferenceModel.Api.Services;

/// <summary>
/// Fallback detection service using simple keyword matching when ML model is unavailable
/// </summary>
public class FallbackDetectionService
{
    private readonly string[] _injectionKeywords = new[]
    {
        "ignore previous",
        "ignore all previous",
        "disregard previous",
        "forget previous",
        "ignore your instructions",
        "ignore the instructions",
        "new instructions",
        "system prompt",
        "reveal prompt",
        "show prompt",
        "pretend you are",
        "act as if",
        "you are now",
        "roleplay as",
        "jailbreak",
        "dan mode",
        "developer mode",
        "unrestricted mode",
        "bypass restrictions",
        "ignore rules",
        "override",
        "sudo mode",
        "admin mode",
        "god mode"
    };

    /// <summary>
    /// Predict using simple keyword matching
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

        var lowerText = text.ToLowerInvariant();
        var matchedKeywords = _injectionKeywords.Count(keyword => lowerText.Contains(keyword));

        // If any injection keywords are found, mark as potential injection
        var isInjection = matchedKeywords > 0;
        
        // Simple confidence scoring based on keyword matches
        var injectionScore = matchedKeywords > 0 ? Math.Min(0.5f + (matchedKeywords * 0.1f), 0.9f) : 0.1f;
        var safeScore = 1.0f - injectionScore;

        return new PromptGuardOutput
        {
            Label = isInjection ? "INJECTION" : "SAFE",
            Score = isInjection ? injectionScore : safeScore,
            AllScores = new Dictionary<string, float>
            {
                { "SAFE", safeScore },
                { "INJECTION", injectionScore }
            }
        };
    }
}



