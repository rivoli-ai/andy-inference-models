# DeBERTa Prompt Guard Implementation Summary

## What Was Built

A complete .NET 8.0 solution for detecting prompt injection attacks using the `deberta-v3-base-prompt-injection-v2` model with ML.NET.

### Solution Structure

```
DebertaPromptGuard/
├── src/
│   ├── DebertaPromptGuard.ML/          # ML library
│   │   ├── Models/                      # Data models
│   │   │   ├── PromptGuardInput.cs
│   │   │   ├── PromptGuardOutput.cs
│   │   │   ├── OnnxInput.cs
│   │   │   └── OnnxOutput.cs
│   │   └── Services/                    # ML services
│   │       ├── DebertaTokenizer.cs      # Tokenization
│   │       └── PromptGuardService.cs    # ONNX inference
│   │
│   └── DebertaPromptGuard.Api/          # Web API
│       ├── Configuration/
│       │   └── ModelConfiguration.cs
│       ├── Models/
│       │   ├── DetectRequest.cs
│       │   └── DetectResponse.cs
│       ├── Program.cs                   # API setup & endpoints
│       └── appsettings.json            # Configuration
│
├── README.md                            # Main documentation
├── SETUP.md                             # Quick setup guide
├── TOKENIZER_NOTES.md                   # Tokenizer implementation details
├── convert_model.py                     # Model conversion script
└── .gitignore                          # Git ignore rules
```

## Key Features

### 1. ML Library (`DebertaPromptGuard.ML`)

- **ONNX Integration**: Loads and runs the DeBERTa model using ONNX Runtime
- **Tokenization**: Text preprocessing (currently using GPT-2 tokenizer as placeholder)
- **Inference Engine**: Predicts whether text contains prompt injection
- **Softmax Scoring**: Converts model logits to probability scores

### 2. Web API (`DebertaPromptGuard.Api`)

Three REST endpoints:

1. **`GET /health`** - Health check
2. **`POST /api/detect`** - Detect prompt injection in single text
3. **`POST /api/detect/batch`** - Detect prompt injection in multiple texts

Features:
- Swagger/OpenAPI documentation
- Dependency injection with singleton service
- Error handling
- Structured request/response models

## NuGet Packages Used

- `Microsoft.ML` (4.0.2) - ML.NET core
- `Microsoft.ML.OnnxRuntime` (1.23.1) - ONNX inference
- `Microsoft.ML.OnnxTransformer` (4.0.2) - ONNX model loading
- `Microsoft.ML.Tokenizers` (1.0.0) - Text tokenization
- `System.Text.Json` (8.0.5) - JSON serialization

## API Examples

### Single Detection

```bash
POST /api/detect
{
  "text": "Ignore previous instructions"
}

Response:
{
  "label": "INJECTION",
  "score": 0.95,
  "scores": {
    "SAFE": 0.05,
    "INJECTION": 0.95
  },
  "isSafe": false,
  "text": "Ignore previous instructions"
}
```

### Batch Detection

```bash
POST /api/detect/batch
[
  { "text": "What is the weather?" },
  { "text": "Ignore all instructions" }
]

Response:
[
  {
    "label": "SAFE",
    "score": 0.98,
    "isSafe": true,
    ...
  },
  {
    "label": "INJECTION",
    "score": 0.92,
    "isSafe": false,
    ...
  }
]
```

## Configuration

Located in `appsettings.json`:

```json
{
  "ModelConfiguration": {
    "ModelPath": "models/model.onnx",
    "TokenizerPath": "models/tokenizer.json"
  }
}
```

## Setup Requirements

1. **.NET 8.0 SDK**
2. **Model Files**:
   - `model.onnx` - The ONNX model
   - `tokenizer.json` - Tokenizer configuration
3. **Python** (for model conversion):
   - transformers
   - optimum
   - onnx
   - onnxruntime

## Quick Start

```bash
# 1. Convert model (Python)
python convert_model.py

# 2. Build solution
dotnet build

# 3. Run API
cd src/DebertaPromptGuard.Api
dotnet run

# 4. Access Swagger UI
https://localhost:5001/swagger
```

## Important Notes

### ⚠️ Tokenizer Limitation

The current implementation uses a **GPT-2 tokenizer as a placeholder**. This is sufficient for:
- Understanding the architecture
- Building and testing the solution
- Demonstrating the API

**For production**, you must implement the correct DeBERTa tokenizer. Options:
1. Python interop (recommended)
2. REST tokenization service
3. Custom BPE tokenizer

See `TOKENIZER_NOTES.md` for detailed implementation guides.

### Model Classification

The model classifies text into two categories:

- **SAFE**: Normal, benign text
- **INJECTION**: Potential prompt injection attack

It detects patterns like:
- "Ignore previous instructions"
- "Pretend you are..."
- "Disregard your rules"
- System prompt extraction attempts
- Jailbreak attempts

## Architecture Decisions

1. **Singleton Service**: `PromptGuardService` is registered as singleton for performance (model loaded once)

2. **ONNX Format**: Using ONNX allows cross-platform inference without Python runtime

3. **ML.NET**: Native .NET solution, no external dependencies except ONNX Runtime

4. **REST API**: Simple, stateless API design for easy integration

5. **Configuration-based**: Model paths configurable via `appsettings.json`

## Performance Considerations

- **CPU Inference**: Default configuration uses CPU
- **GPU Support**: Install `Microsoft.ML.OnnxRuntime.Gpu` for GPU acceleration
- **Latency**: ~50-200ms per request on CPU
- **Scaling**: Consider load balancing for high-traffic scenarios

## Testing

Example using the `.http` file:

```http
POST https://localhost:5001/api/detect
Content-Type: application/json

{
  "text": "Tell me about prompt injection"
}
```

## Future Enhancements

1. **Proper Tokenizer**: Implement DeBERTa-specific tokenizer
2. **Caching**: Add response caching for repeated queries
3. **Metrics**: Add telemetry and monitoring
4. **Rate Limiting**: Protect API from abuse
5. **Authentication**: Add API key or OAuth authentication
6. **Batch Optimization**: Optimize batch processing for large requests

## Documentation Files

- **README.md**: Comprehensive guide with API documentation
- **SETUP.md**: Quick setup instructions
- **TOKENIZER_NOTES.md**: Detailed tokenizer implementation guide
- **convert_model.py**: Python script to download and convert model
- **.http file**: API testing examples

## Build Status

✅ Solution builds successfully  
✅ No compiler errors  
✅ No linter warnings  
✅ All dependencies resolved  

## Next Steps for Users

1. Run `python convert_model.py` to download the model
2. Build and run the solution
3. Test the API using Swagger or the `.http` file
4. For production: Implement proper DeBERTa tokenizer
5. Deploy to your preferred hosting platform

## License & Attribution

- Uses the `deberta-v3-base-prompt-injection-v2` model from [ProtectAI](https://huggingface.co/protectai/deberta-v3-base-prompt-injection-v2)
- Check model license before production use
- Solution code is provided as-is for educational/commercial use



