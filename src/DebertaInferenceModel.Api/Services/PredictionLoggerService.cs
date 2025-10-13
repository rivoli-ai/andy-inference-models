using DebertaInferenceModel.Api.Models;
using System.Collections.Concurrent;

namespace DebertaInferenceModel.Api.Services;

/// <summary>
/// Service for logging prediction requests and results
/// </summary>
public class PredictionLoggerService
{
    private readonly ConcurrentQueue<PredictionLog> _logs = new();
    private readonly int _maxLogSize;

    public PredictionLoggerService(int maxLogSize = 1000)
    {
        _maxLogSize = maxLogSize;
    }

    /// <summary>
    /// Add a prediction log entry
    /// </summary>
    public void LogPrediction(PredictionLog log)
    {
        _logs.Enqueue(log);

        // Maintain max size by dequeuing old entries
        while (_logs.Count > _maxLogSize)
        {
            _logs.TryDequeue(out _);
        }
    }

    /// <summary>
    /// Get all logs
    /// </summary>
    public IEnumerable<PredictionLog> GetAllLogs()
    {
        return _logs.ToArray().OrderByDescending(l => l.Timestamp);
    }

    /// <summary>
    /// Get logs with filtering and pagination
    /// </summary>
    public IEnumerable<PredictionLog> GetLogs(
        string? label = null,
        int skip = 0,
        int take = 100)
    {
        var query = _logs.AsEnumerable();

        if (!string.IsNullOrEmpty(label))
        {
            query = query.Where(l => l.Label.Equals(label, StringComparison.OrdinalIgnoreCase));
        }

        return query
            .OrderByDescending(l => l.Timestamp)
            .Skip(skip)
            .Take(Math.Min(take, 1000))
            .ToArray();
    }

    /// <summary>
    /// Get statistics about predictions
    /// </summary>
    public object GetStatistics()
    {
        var logs = _logs.ToArray();
        
        if (logs.Length == 0)
        {
            return new
            {
                totalPredictions = 0,
                safeCount = 0,
                injectionCount = 0,
                averageResponseTimeMs = 0.0,
                averageConfidence = 0.0,
                mlModelUsed = 0,
                fallbackUsed = 0
            };
        }

        var safeCount = logs.Count(l => l.Label == "SAFE");
        var injectionCount = logs.Count(l => l.Label == "INJECTION");
        var mlModelCount = logs.Count(l => !l.UsedFallback);
        var fallbackCount = logs.Count(l => l.UsedFallback);

        return new
        {
            totalPredictions = logs.Length,
            safeCount,
            injectionCount,
            safePercentage = Math.Round((double)safeCount / logs.Length * 100, 2),
            injectionPercentage = Math.Round((double)injectionCount / logs.Length * 100, 2),
            averageResponseTimeMs = Math.Round(logs.Average(l => l.ResponseTimeMs), 2),
            averageConfidence = Math.Round(logs.Average(l => l.Score), 4),
            oldestLog = logs.Min(l => l.Timestamp),
            newestLog = logs.Max(l => l.Timestamp),
            mlModelUsed = mlModelCount,
            fallbackUsed = fallbackCount,
            mlModelPercentage = Math.Round((double)mlModelCount / logs.Length * 100, 2),
            fallbackPercentage = Math.Round((double)fallbackCount / logs.Length * 100, 2)
        };
    }

    /// <summary>
    /// Clear all logs
    /// </summary>
    public void ClearLogs()
    {
        _logs.Clear();
    }

    /// <summary>
    /// Get total count of logs
    /// </summary>
    public int GetCount()
    {
        return _logs.Count;
    }
}

