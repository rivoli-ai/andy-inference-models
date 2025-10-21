using Microsoft.ML.Tokenizers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InferenceModel.ML.Services;

/// <summary>
/// Tokenizer for ML models with Python microservice support
/// Calls Python tokenizer service for accurate HuggingFace tokenization, falls back to GPT-2 if service unavailable
/// Supports multiple models: DeBERTa, GraphCodeBERT, etc.
/// </summary>
public class ModelTokenizer
{
    private readonly HttpClient _httpClient;
    private readonly string? _tokenizerServiceUrl;
    private readonly Tokenizer? _fallbackTokenizer;
    private readonly int _maxLength;
    private readonly bool _usingFallback;
    private readonly bool _useTokenizerService;
    private readonly string _modelId;
    
    public bool IsUsingFallback => _usingFallback;

    public ModelTokenizer(string tokenizerJsonPathOrServiceUrl, string modelId = "deberta-v3-base-prompt-injection-v2", int maxLength = 512)
    {
        _maxLength = maxLength;
        _modelId = modelId;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        
        // Check if this is a URL (tokenizer service) or a file path
        if (tokenizerJsonPathOrServiceUrl.StartsWith("http://") || tokenizerJsonPathOrServiceUrl.StartsWith("https://"))
        {
            _tokenizerServiceUrl = tokenizerJsonPathOrServiceUrl.TrimEnd('/');
            _useTokenizerService = true;
            _usingFallback = false;
            
            Console.WriteLine($"🔗 Configuring to use Python tokenizer service: {_tokenizerServiceUrl}");
            
            // Try to connect to the service
            try
            {
                var healthTask = _httpClient.GetAsync($"{_tokenizerServiceUrl}/health");
                healthTask.Wait(TimeSpan.FromSeconds(5));
                
                if (healthTask.IsCompletedSuccessfully && healthTask.Result.IsSuccessStatusCode)
                {
                    Console.WriteLine("✓ Python tokenizer service is healthy");
                    Console.WriteLine($"  Using 100% accurate HuggingFace tokenization for: {_modelId}");
                    return;
                }
                else
                {
                    Console.WriteLine("⚠ Python tokenizer service not responding, will use fallback");
                    _useTokenizerService = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Cannot connect to tokenizer service: {ex.Message}");
                Console.WriteLine("  Falling back to GPT-2 tokenizer");
                _useTokenizerService = false;
            }
        }
        else
        {
            _useTokenizerService = false;
            Console.WriteLine($"📁 Tokenizer path provided: {tokenizerJsonPathOrServiceUrl}");
            
            // Original file-based logic
            if (File.Exists(tokenizerJsonPathOrServiceUrl))
            {
                Console.WriteLine("⚠ DeBERTa tokenizer.json format not supported by Microsoft.ML.Tokenizers");
                Console.WriteLine("  To use accurate tokenization, configure TokenizerServiceUrl in appsettings.json");
                Console.WriteLine("  See TOKENIZER_IMPLEMENTATION_GUIDE.md for details");
            }
            else
            {
                Console.WriteLine($"⚠ Tokenizer file not found: {tokenizerJsonPathOrServiceUrl}");
            }
        }
        
        // Initialize fallback tokenizer
        try
        {
            _fallbackTokenizer = TiktokenTokenizer.CreateForModel("gpt2");
            _usingFallback = true;
            Console.WriteLine("✓ Using GPT-2 tokenizer as fallback (accuracy may be reduced)");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to initialize fallback tokenizer: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Tokenize text and return input IDs and attention mask
    /// Uses Python tokenizer service for accurate tokenization, falls back to GPT-2 if unavailable
    /// </summary>
    public (long[] InputIds, long[] AttentionMask) Encode(string text)
    {
        // Try to use tokenizer service first
        if (_useTokenizerService && _tokenizerServiceUrl != null)
        {
            try
            {
                return EncodeWithService(text);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Tokenizer service error: {ex.Message}");
                Console.WriteLine("  Falling back to local tokenizer");
            }
        }
        
        // Fallback to local GPT-2 tokenizer
        return EncodeWithFallback(text);
    }
    
    private (long[] InputIds, long[] AttentionMask) EncodeWithService(string text)
    {
        var request = new TokenizeRequest 
        { 
            Text = text, 
            MaxLength = _maxLength,
            Model = _modelId
        };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        
        var responseTask = _httpClient.PostAsync($"{_tokenizerServiceUrl}/tokenize", content);
        responseTask.Wait();
        
        if (!responseTask.Result.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Tokenizer service returned {responseTask.Result.StatusCode}");
        }
        
        var responseContentTask = responseTask.Result.Content.ReadAsStringAsync();
        responseContentTask.Wait();
        
        var response = JsonSerializer.Deserialize<TokenizeResponse>(responseContentTask.Result);
        
        if (response == null)
        {
            throw new InvalidOperationException("Failed to deserialize tokenizer response");
        }
        
        return (response.InputIds, response.AttentionMask);
    }
    
    private (long[] InputIds, long[] AttentionMask) EncodeWithFallback(string text)
    {
        if (_fallbackTokenizer == null)
        {
            throw new InvalidOperationException("Fallback tokenizer not initialized");
        }
        
        // Encode the text as-is (preserving capitalization)
        var ids = _fallbackTokenizer.EncodeToIds(text);
        
        // Truncate or pad to max length
        var inputIds = new long[_maxLength];
        var attentionMask = new long[_maxLength];
        
        var length = Math.Min(ids.Count, _maxLength);
        
        for (int i = 0; i < length; i++)
        {
            inputIds[i] = ids[i];
            attentionMask[i] = 1;
        }
        
        return (inputIds, attentionMask);
    }
    
    // JSON models for tokenizer service
    private class TokenizeRequest
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
        
        [JsonPropertyName("max_length")]
        public int MaxLength { get; set; }
        
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;
    }
    
    private class TokenizeResponse
    {
        [JsonPropertyName("input_ids")]
        public long[] InputIds { get; set; } = Array.Empty<long>();
        
        [JsonPropertyName("attention_mask")]
        public long[] AttentionMask { get; set; } = Array.Empty<long>();
        
        [JsonPropertyName("token_count")]
        public int TokenCount { get; set; }
    }
}


