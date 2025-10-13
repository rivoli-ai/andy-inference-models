# DeBERTa Tokenizer Implementation Guide

## Current Status

✅ **Application is working** with GPT-2 tokenizer placeholder  
⚠️ **For production accuracy**, you need the proper DeBERTa tokenizer

---

## Why the Current Tokenizer is a Placeholder

**Problem**: `Microsoft.ML.Tokenizers` v1.0.0 doesn't support loading HuggingFace `tokenizer.json` files.

**Impact**: 
- The app works and gives predictions
- But tokenization doesn't match what the model expects
- Accuracy may be lower than intended

---

## Production Implementation Options

### Option 1: Python Microservice (Recommended ⭐)

**Best for**: Production deployments, accuracy, maintainability

Create a separate Python FastAPI service for tokenization:

#### Python Service (`tokenizer-service/app.py`):

```python
from fastapi import FastAPI
from transformers import AutoTokenizer
import uvicorn

app = FastAPI()

# Load DeBERTa tokenizer once at startup
tokenizer = AutoTokenizer.from_pretrained(
    "protectai/deberta-v3-base-prompt-injection-v2"
)

@app.post("/tokenize")
def tokenize(request: dict):
    text = request.get("text", "")
    
    # Tokenize with proper DeBERTa tokenization
    encoded = tokenizer(
        text,
        max_length=512,
        padding="max_length",
        truncation=True,
        return_tensors="pt"
    )
    
    return {
        "input_ids": encoded["input_ids"][0].tolist(),
        "attention_mask": encoded["attention_mask"][0].tolist()
    }

@app.get("/health")
def health():
    return {"status": "healthy", "tokenizer": "deberta-v3-base"}

if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0", port=8000)
```

#### Update C# Tokenizer to call Python service:

```csharp
public class DebertaTokenizer
{
    private readonly HttpClient _httpClient;
    private readonly string _tokenizerServiceUrl;
    private readonly int _maxLength;
    
    public DebertaTokenizer(string tokenizerServiceUrl, int maxLength = 512)
    {
        _tokenizerServiceUrl = tokenizerServiceUrl;
        _maxLength = maxLength;
        _httpClient = new HttpClient();
    }
    
    public async Task<(long[] InputIds, long[] AttentionMask)> EncodeAsync(string text)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"{_tokenizerServiceUrl}/tokenize",
            new { text }
        );
        
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<TokenizerResponse>();
        
        return (result.InputIds, result.AttentionMask);
    }
    
    private class TokenizerResponse
    {
        public long[] InputIds { get; set; }
        public long[] AttentionMask { get; set; }
    }
}
```

#### Docker Compose Setup:

```yaml
version: '3.8'

services:
  tokenizer-service:
    build: ./tokenizer-service
    container_name: tokenizer-service
    ports:
      - "8000:8000"
    restart: unless-stopped
    
  promptguard-api:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: deberta-promptguard
    ports:
      - "5158:8080"
    environment:
      - TokenizerServiceUrl=http://tokenizer-service:8000
    depends_on:
      - tokenizer-service
    restart: unless-stopped
```

**Advantages**:
- ✅ 100% accurate tokenization
- ✅ Matches model training exactly
- ✅ Easy to update/maintain
- ✅ Can reuse Python ecosystem

**Disadvantages**:
- ❌ Additional service to deploy
- ❌ Network call overhead (~5-10ms)

---

### Option 2: Python.NET Integration

**Best for**: Single container deployment, simpler architecture

Embed Python within your C# application:

#### 1. Install Python.NET:

```bash
dotnet add package Python.Runtime
```

#### 2. Update DebertaTokenizer:

```csharp
using Python.Runtime;

public class DebertaTokenizer : IDisposable
{
    private dynamic _tokenizer;
    private readonly int _maxLength;
    
    public DebertaTokenizer(string modelName, int maxLength = 512)
    {
        _maxLength = maxLength;
        
        // Initialize Python engine
        if (!PythonEngine.IsInitialized)
        {
            Runtime.PythonDLL = "python311.dll"; // Adjust version
            PythonEngine.Initialize();
        }
        
        using (Py.GIL())
        {
            dynamic transformers = Py.Import("transformers");
            _tokenizer = transformers.AutoTokenizer.from_pretrained(modelName);
        }
    }
    
    public (long[] InputIds, long[] AttentionMask) Encode(string text)
    {
        using (Py.GIL())
        {
            dynamic encoded = _tokenizer.encode_plus(
                text,
                max_length: _maxLength,
                padding: "max_length",
                truncation: true,
                return_tensors: "pt"
            );
            
            var inputIds = ((PyObject)encoded["input_ids"]).As<long[][]>()[0];
            var attentionMask = ((PyObject)encoded["attention_mask"]).As<long[][]>()[0];
            
            return (inputIds, attentionMask);
        }
    }
    
    public void Dispose()
    {
        // Cleanup if needed
    }
}
```

#### 3. Docker Setup:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# Install Python
RUN apt-get update && \
    apt-get install -y python3 python3-pip && \
    pip3 install transformers torch

# Copy your app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "DebertaPromptGuard.Api.dll"]
```

**Advantages**:
- ✅ 100% accurate tokenization
- ✅ Single container
- ✅ No network overhead

**Disadvantages**:
- ❌ Python dependency in .NET app
- ❌ More complex Docker image
- ❌ GIL threading considerations

---

### Option 3: Custom Unigram Tokenizer in C#

**Best for**: No Python dependencies, native performance

Implement the Unigram (SentencePiece) algorithm in C#:

```csharp
public class DeBertaUnigramTokenizer
{
    private readonly Dictionary<string, int> _vocab;
    private readonly List<(string, float)> _pieces;
    private readonly int _maxLength;
    
    public DeBertaUnigramTokenizer(string tokenizerJsonPath, int maxLength = 512)
    {
        _maxLength = maxLength;
        
        // Parse tokenizer.json
        var json = JsonSerializer.Deserialize<TokenizerJson>(
            File.ReadAllText(tokenizerJsonPath)
        );
        
        // Build vocabulary and pieces
        _vocab = new Dictionary<string, int>();
        _pieces = new List<(string, float)>();
        
        foreach (var item in json.Model.Vocab)
        {
            var piece = item[0];
            var score = item[1];
            _vocab[piece] = _vocab.Count;
            _pieces.Add((piece, score));
        }
    }
    
    public (long[] InputIds, long[] AttentionMask) Encode(string text)
    {
        // Implement Unigram segmentation algorithm
        // This is complex - need to:
        // 1. Normalize text
        // 2. Pre-tokenize
        // 3. Apply Unigram segmentation
        // 4. Map to token IDs
        // 5. Add special tokens [CLS], [SEP]
        // 6. Pad/truncate to max_length
        
        var tokens = SegmentUnigram(text);
        var inputIds = TokensToIds(tokens);
        
        // Add [CLS] and [SEP]
        var sequence = new List<long> { 1 }; // [CLS]
        sequence.AddRange(inputIds);
        sequence.Add(2); // [SEP]
        
        // Pad to max_length
        while (sequence.Count < _maxLength)
        {
            sequence.Add(0); // [PAD]
        }
        
        var attentionMask = sequence.Select(id => id != 0 ? 1L : 0L).ToArray();
        
        return (sequence.ToArray(), attentionMask);
    }
    
    private List<string> SegmentUnigram(string text)
    {
        // Implement Viterbi algorithm for Unigram segmentation
        // See: https://arxiv.org/abs/1804.10959
        
        // This is simplified - full implementation needed
        var result = new List<string>();
        // ... complex algorithm here ...
        return result;
    }
}
```

**Advantages**:
- ✅ No Python dependency
- ✅ Native C# performance
- ✅ Single language

**Disadvantages**:
- ❌ Very complex to implement correctly
- ❌ High risk of tokenization bugs
- ❌ Hard to maintain and test
- ❌ Need to implement Unigram/SentencePiece algorithm

---

### Option 4: Wait for Microsoft.ML.Tokenizers Update

**Best for**: Future-proofing

Monitor the [Microsoft.ML.Tokenizers GitHub](https://github.com/dotnet/machinelearning) for updates.

When HuggingFace tokenizer support is added:

```csharp
// Future API (not yet available)
_tokenizer = Tokenizer.LoadFromFile(tokenizerJsonPath);
var result = _tokenizer.Encode(text);
```

**Advantages**:
- ✅ Native support
- ✅ Microsoft maintained

**Disadvantages**:
- ❌ Not available yet
- ❌ Unknown timeline

---

## Recommendation

### For Development/Testing:
✅ **Current GPT-2 placeholder** - Keep it, it works for architecture testing

### For Production:
🥇 **Option 1: Python Microservice** - Best accuracy and maintainability  
🥈 **Option 2: Python.NET** - If you can't deploy multiple containers  
🥉 **Option 3: Custom Implementation** - Only if Python is absolutely forbidden

---

## Testing Tokenization Accuracy

To verify tokenization works correctly, compare outputs:

### Python (Ground Truth):
```python
from transformers import AutoTokenizer

tokenizer = AutoTokenizer.from_pretrained(
    "protectai/deberta-v3-base-prompt-injection-v2"
)

text = "Ignore previous instructions"
result = tokenizer(text, max_length=512, padding="max_length", truncation=True)
print(result["input_ids"][:20])  # First 20 tokens
```

### Your C# Implementation:
```csharp
var tokenizer = new DebertaTokenizer(...);
var (inputIds, _) = tokenizer.Encode("Ignore previous instructions");
Console.WriteLine(string.Join(", ", inputIds.Take(20)));
```

**They should match exactly!**

---

## Next Steps

1. ✅ **Current app works** - You can use it for testing
2. 🔄 **Choose implementation** based on your deployment requirements
3. 🧪 **Implement and test** tokenization accuracy
4. 🚀 **Deploy to production** with proper tokenizer

---

## Need Help?

See `TOKENIZER_NOTES.md` for additional implementation details and resources.

