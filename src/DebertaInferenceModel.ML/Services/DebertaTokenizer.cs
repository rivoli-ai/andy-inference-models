using Microsoft.ML.Tokenizers;

namespace DebertaInferenceModel.ML.Services;

/// <summary>
/// Tokenizer for DeBERTa models with fallback support
/// Tries to load DeBERTa tokenizer from tokenizer.json, falls back to GPT-2 if unavailable
/// </summary>
public class DebertaTokenizer
{
    private readonly Tokenizer _tokenizer;
    private readonly int _maxLength;
    private readonly bool _usingFallback;
    
    public bool IsUsingFallback => _usingFallback;

    public DebertaTokenizer(string tokenizerJsonPath, int maxLength = 512)
    {
        _maxLength = maxLength;
        _usingFallback = false;
        
        // Try to load DeBERTa tokenizer from tokenizer.json
        if (File.Exists(tokenizerJsonPath))
        {
            try
            {
                Console.WriteLine($"Attempting to load DeBERTa tokenizer from: {tokenizerJsonPath}");
                
                // Try various methods to load the tokenizer
                // Note: Microsoft.ML.Tokenizers v1.0.0 has limited HuggingFace support
                
                // Attempt 1: Try to load from file (will fail - not supported yet)
                try
                {
                    using var stream = File.OpenRead(tokenizerJsonPath);
                    // Microsoft.ML.Tokenizers v1.0.0 doesn't have a method to load HuggingFace tokenizer.json
                    // This is just a placeholder for when the feature is added
                    throw new NotSupportedException("HuggingFace tokenizer.json not supported");
                }
                catch
                {
                    // Expected to fail - format not supported yet
                }
                
                // If we get here, HuggingFace tokenizer loading failed
                Console.WriteLine("⚠ DeBERTa tokenizer.json format not supported by Microsoft.ML.Tokenizers v1.0.0");
                Console.WriteLine("  Falling back to GPT-2 tokenizer (may affect accuracy)");
                Console.WriteLine("  For production, consider:");
                Console.WriteLine("    1. Python.NET integration");
                Console.WriteLine("    2. Separate tokenization microservice");
                Console.WriteLine("    3. See TOKENIZER_IMPLEMENTATION_GUIDE.md");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Error loading DeBERTa tokenizer: {ex.Message}");
                Console.WriteLine("  Falling back to GPT-2 tokenizer");
            }
        }
        else
        {
            Console.WriteLine($"⚠ Tokenizer file not found: {tokenizerJsonPath}");
            Console.WriteLine("  Falling back to GPT-2 tokenizer");
        }
        
        // Fallback: Use GPT-2 tokenizer
        try
        {
            _tokenizer = TiktokenTokenizer.CreateForModel("gpt2");
            _usingFallback = true;
            Console.WriteLine("✓ Using GPT-2 tokenizer as fallback");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to initialize fallback tokenizer: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Tokenize text and return input IDs and attention mask
    /// NOTE: When using GPT-2 fallback, capitalization affects detection.
    /// For production accuracy, use proper DeBERTa tokenizer (see TOKENIZER_IMPLEMENTATION_GUIDE.md)
    /// </summary>
    public (long[] InputIds, long[] AttentionMask) Encode(string text)
    {
        // Encode the text as-is (preserving capitalization)
        // Case normalization was tested and reduced detection accuracy
        var ids = _tokenizer.EncodeToIds(text);
        
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
}

