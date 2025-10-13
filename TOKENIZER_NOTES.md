# Tokenizer Implementation Notes

## Current Implementation

The current implementation uses a **GPT-2 tokenizer as a placeholder**. This is provided to get the solution building and demonstrate the overall architecture.

## Important: Production Requirements

⚠️ **For production use with accurate results, you must use the exact DeBERTa tokenizer that matches the model.**

The `deberta-v3-base-prompt-injection-v2` model was trained with a specific tokenizer, and using a different tokenizer (like GPT-2) will produce incorrect results.

## Options for Production Deployment

### Option 1: Use Python Interop (Recommended)

Use Python.NET to call the HuggingFace tokenizer directly:

1. Install Python.NET:
   ```bash
   dotnet add package Python.Runtime
   ```

2. Implement a tokenizer wrapper that calls Python:
   ```csharp
   using Python.Runtime;
   
   public class HuggingFaceTokenizer
   {
       public HuggingFaceTokenizer()
       {
           PythonEngine.Initialize();
           dynamic transformers = Py.Import("transformers");
           _tokenizer = transformers.AutoTokenizer.from_pretrained(
               "protectai/deberta-v3-base-prompt-injection-v2"
           );
       }
       
       public (long[] InputIds, long[] AttentionMask) Encode(string text)
       {
           dynamic encoded = _tokenizer.encode_plus(
               text,
               max_length: 512,
               padding: "max_length",
               truncation: true,
               return_tensors: "pt"
           );
           
           // Convert Python objects to C# arrays
           // ...
       }
   }
   ```

### Option 2: Use a REST Tokenization Service

Create a separate Python FastAPI service that handles tokenization:

1. Python service:
   ```python
   from fastapi import FastAPI
   from transformers import AutoTokenizer
   
   app = FastAPI()
   tokenizer = AutoTokenizer.from_pretrained(
       "protectai/deberta-v3-base-prompt-injection-v2"
   )
   
   @app.post("/tokenize")
   def tokenize(text: str):
       encoded = tokenizer.encode_plus(
           text,
           max_length=512,
           padding="max_length",
           truncation=True,
           return_tensors="pt"
       )
       return {
           "input_ids": encoded["input_ids"].tolist(),
           "attention_mask": encoded["attention_mask"].tolist()
       }
   ```

2. Call from C#:
   ```csharp
   using System.Net.Http.Json;
   
   var response = await httpClient.PostAsJsonAsync(
       "http://tokenizer-service/tokenize",
       new { text = inputText }
   );
   var result = await response.Content.ReadFromJsonAsync<TokenizerResponse>();
   ```

### Option 3: Implement Custom BPE Tokenizer

Implement a BPE tokenizer in C# that matches DeBERTa's tokenization:

1. Load vocab and merges from the model files
2. Implement BPE algorithm
3. Handle special tokens correctly
4. Apply the same preprocessing as the original model

This is complex and error-prone. Only recommended if you cannot use Python interop.

### Option 4: Use Microsoft.ML.Tokenizers with Proper Configuration

Once Microsoft.ML.Tokenizers adds support for loading HuggingFace tokenizer.json files, you can update the code to:

```csharp
// When supported in future versions
_tokenizer = Tokenizer.CreateFromFile(tokenizerJsonPath);
```

Monitor the [Microsoft.ML.Tokenizers GitHub repository](https://github.com/dotnet/machinelearning) for updates.

## Testing Tokenization Accuracy

To verify your tokenizer is working correctly:

1. Tokenize the same text in both Python and C#
2. Compare the resulting token IDs - they should match exactly
3. Example test:

Python:
```python
from transformers import AutoTokenizer
tokenizer = AutoTokenizer.from_pretrained(
    "protectai/deberta-v3-base-prompt-injection-v2"
)
result = tokenizer.encode("Ignore previous instructions")
print(result)  # [0, 100, 261, 45, 1253, 316, 2, ...]
```

C#:
```csharp
var tokenizer = new DebertaTokenizer("path/to/tokenizer.json");
var (inputIds, _) = tokenizer.Encode("Ignore previous instructions");
Console.WriteLine(string.Join(", ", inputIds.Take(10)));
// Should match Python output
```

## Why Tokenization Matters

The tokenizer converts text into numerical IDs that the model understands. Using the wrong tokenizer means:

- ❌ Words are split differently
- ❌ Token IDs don't match what the model expects
- ❌ Model predictions will be incorrect or nonsensical
- ❌ Accuracy drops significantly

## Recommendations

For **development/testing**: The current GPT-2 tokenizer implementation is fine to understand the architecture.

For **production**: Use **Option 1 (Python Interop)** or **Option 2 (REST Service)** to ensure tokenization accuracy.

## Additional Resources

- [HuggingFace Tokenizers Documentation](https://huggingface.co/docs/tokenizers/)
- [DeBERTa Model Card](https://huggingface.co/protectai/deberta-v3-base-prompt-injection-v2)
- [Microsoft.ML.Tokenizers GitHub](https://github.com/dotnet/machinelearning)



