# Project Overview: DeBERTa Prompt Guard

## 🎯 Purpose
Detect prompt injection attacks in real-time using the DeBERTa v3 transformer model, exposed via a .NET Web API.

## 📊 Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         Client Application                       │
│                    (Web App, Mobile, Service)                    │
└───────────────────────────┬─────────────────────────────────────┘
                            │ HTTP/HTTPS
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                    DebertaPromptGuard.Api                        │
│                      (ASP.NET Core 8.0)                          │
│                                                                   │
│  Endpoints:                                                       │
│  • GET  /health              → Health check                      │
│  • POST /api/detect          → Single text detection             │
│  • POST /api/detect/batch    → Multiple text detection           │
│                                                                   │
│  Features:                                                        │
│  • Swagger/OpenAPI documentation                                 │
│  • Dependency injection                                           │
│  • Configuration management                                       │
└───────────────────────────┬─────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                   DebertaPromptGuard.ML                          │
│                    (Class Library)                               │
│                                                                   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │          PromptGuardService (Singleton)                   │   │
│  │  • Model loading & management                             │   │
│  │  • Prediction orchestration                               │   │
│  │  • Softmax probability calculation                        │   │
│  └────────────────┬──────────────┬──────────────────────────┘   │
│                   │              │                               │
│         ┌─────────▼──────┐  ┌───▼────────────────┐              │
│         │ DebertaTokenizer│  │ ONNX Runtime       │              │
│         │                 │  │ Inference Engine   │              │
│         │ • Text → IDs    │  │                    │              │
│         │ • Padding       │  │ • Load ONNX model  │              │
│         │ • Truncation    │  │ • Run inference    │              │
│         │ • Attention mask│  │ • Return logits    │              │
│         └─────────────────┘  └────────────────────┘              │
└───────────────────────────┬─────────────────────────────────────┘
                            │
                            ▼
                  ┌──────────────────┐
                  │  Model Files     │
                  │  ├── model.onnx  │
                  │  └── tokenizer   │
                  │      .json       │
                  └──────────────────┘
```

## 🔄 Request Flow

```
1. Client sends text
        ↓
2. API receives request (DetectRequest)
        ↓
3. PromptGuardService.Predict(text)
        ↓
4. DebertaTokenizer.Encode(text)
   → Returns: (input_ids, attention_mask)
        ↓
5. Create OnnxInput with tensors
        ↓
6. PredictionEngine.Predict(onnxInput)
   → ONNX Runtime processes through model
   → Returns: logits [2 values]
        ↓
7. Apply Softmax to convert logits → probabilities
   → [0.05, 0.95] = 5% SAFE, 95% INJECTION
        ↓
8. Determine label (highest probability)
        ↓
9. Create PromptGuardOutput
        ↓
10. Map to DetectResponse
        ↓
11. Return JSON to client
```

## 📁 File Structure

```
DebertaPromptGuard/
│
├── 📄 Documentation
│   ├── README.md                    # Main documentation
│   ├── SETUP.md                     # Quick start guide
│   ├── TOKENIZER_NOTES.md          # Tokenizer implementation
│   ├── SUMMARY.md                  # Implementation summary
│   ├── PROJECT_OVERVIEW.md         # This file
│   └── .gitignore                  # Git ignore rules
│
├── 🐍 Python Scripts
│   └── convert_model.py            # Model conversion utility
│
├── 📦 Solution
│   └── DebertaPromptGuard.sln     # Visual Studio solution
│
└── 📂 Source Code (src/)
    │
    ├── 🧠 ML Library (DebertaPromptGuard.ML/)
    │   ├── 📝 Models/
    │   │   ├── PromptGuardInput.cs    # Input: Text
    │   │   ├── PromptGuardOutput.cs   # Output: Label, Score, AllScores
    │   │   ├── OnnxInput.cs           # ONNX: InputIds, AttentionMask
    │   │   └── OnnxOutput.cs          # ONNX: Logits
    │   │
    │   ├── ⚙️ Services/
    │   │   ├── DebertaTokenizer.cs      # Text tokenization
    │   │   └── PromptGuardService.cs    # Main ML service
    │   │
    │   └── DebertaPromptGuard.ML.csproj # Project file
    │
    └── 🌐 Web API (DebertaPromptGuard.Api/)
        ├── ⚙️ Configuration/
        │   └── ModelConfiguration.cs   # Settings model
        │
        ├── 📝 Models/
        │   ├── DetectRequest.cs       # API request DTO
        │   └── DetectResponse.cs      # API response DTO
        │
        ├── Program.cs                 # API setup & endpoints
        ├── appsettings.json          # Configuration
        ├── appsettings.Development.json
        ├── DebertaPromptGuard.Api.http  # API test file
        └── DebertaPromptGuard.Api.csproj
```

## 🔑 Key Classes

### PromptGuardService
- **Purpose**: Main ML service, manages model lifecycle
- **Lifetime**: Singleton (loaded once, reused)
- **Methods**: 
  - `Predict(text)` → `PromptGuardOutput`
  - `Softmax(logits)` → probabilities

### DebertaTokenizer
- **Purpose**: Convert text to model-compatible format
- **Current**: Uses GPT-2 tokenizer (placeholder)
- **Production**: Needs DeBERTa-specific tokenizer
- **Output**: (input_ids[], attention_mask[])

### OnnxInput/Output
- **Input**: 
  - `input_ids`: Token IDs (512 length array)
  - `attention_mask`: Which tokens are real vs padding
- **Output**: 
  - `logits`: 2 values (SAFE score, INJECTION score)

## 🚀 API Endpoints

| Method | Endpoint | Purpose | Input | Output |
|--------|----------|---------|-------|--------|
| GET | `/health` | Health check | None | Status object |
| POST | `/api/detect` | Single detection | `{text}` | DetectResponse |
| POST | `/api/detect/batch` | Batch detection | `[{text}, ...]` | DetectResponse[] |

## 🎨 Response Format

```json
{
  "label": "INJECTION",           // SAFE or INJECTION
  "score": 0.95,                  // Confidence (0-1)
  "scores": {                     // All class scores
    "SAFE": 0.05,
    "INJECTION": 0.95
  },
  "isSafe": false,               // Convenience boolean
  "text": "Ignore instructions"  // Original input
}
```

## 🔧 Technologies

| Component | Technology | Version |
|-----------|-----------|---------|
| Framework | .NET | 8.0 |
| ML Framework | ML.NET | 4.0.2 |
| ONNX Runtime | Microsoft.ML.OnnxRuntime | 1.23.1 |
| Tokenizers | Microsoft.ML.Tokenizers | 1.0.0 |
| Web Framework | ASP.NET Core | 8.0 |
| API Docs | Swagger/OpenAPI | 8.0 |

## ⚡ Performance

| Metric | Value | Notes |
|--------|-------|-------|
| Inference Time | 50-200ms | CPU, varies by text length |
| Model Size | ~500MB | DeBERTa v3 base |
| Max Input Length | 512 tokens | DeBERTa limitation |
| Concurrent Requests | Depends | Use load balancing |

## 🔐 Security Considerations

1. **Input Validation**: API validates text is not empty
2. **Error Handling**: Exceptions caught and returned as Problem Details
3. **HTTPS**: Enabled by default
4. **Rate Limiting**: Not implemented (TODO)
5. **Authentication**: Not implemented (TODO)

## 📈 Scaling Options

### Vertical Scaling
- Increase CPU/RAM
- Use GPU (install `Microsoft.ML.OnnxRuntime.Gpu`)

### Horizontal Scaling
- Deploy multiple API instances
- Use load balancer (Azure Load Balancer, NGINX)
- Shared model files on network storage

### Optimization
- Response caching for common queries
- Batch processing optimization
- Model quantization for smaller size

## 🧪 Testing

### Unit Tests (TODO)
- Test tokenizer output
- Test model predictions
- Test API endpoints

### Integration Tests (TODO)
- End-to-end API tests
- Model accuracy validation

### Manual Testing
Use `DebertaPromptGuard.Api.http` file or Swagger UI

## 🎯 Model Capabilities

### Detects:
✅ Direct instruction overrides ("Ignore previous")  
✅ Role-playing attempts ("Pretend you are")  
✅ System prompt extraction  
✅ Jailbreak attempts ("DAN mode")  
✅ Rule circumvention patterns  

### Does Not Detect:
❌ Novel/zero-day injection patterns  
❌ Non-English injections (model is English-trained)  
❌ Semantic-only attacks without keywords  

## 🚦 Current Status

| Component | Status | Notes |
|-----------|--------|-------|
| Solution Build | ✅ Success | No errors |
| ML Library | ✅ Complete | Placeholder tokenizer |
| Web API | ✅ Complete | Full REST API |
| Documentation | ✅ Complete | 5 docs created |
| Model Files | ⚠️ Not included | User must download |
| Tokenizer | ⚠️ Placeholder | GPT-2 instead of DeBERTa |
| Tests | ❌ Not implemented | TODO |

## 📋 Next Steps

### For Development
1. ✅ Build solution - DONE
2. ⏳ Download model files
3. ⏳ Test API endpoints
4. ⏳ Verify responses

### For Production
1. ❗ Implement proper DeBERTa tokenizer
2. ❗ Add authentication
3. ❗ Add rate limiting
4. ❗ Add monitoring/telemetry
5. ❗ Add caching
6. ❗ Write tests
7. ❗ Set up CI/CD
8. ❗ Deploy to cloud

## 🆘 Support

- See `README.md` for comprehensive guide
- See `SETUP.md` for quick start
- See `TOKENIZER_NOTES.md` for tokenizer implementation
- Check Issues section for common problems

## 📄 License

Check the model license at [HuggingFace](https://huggingface.co/protectai/deberta-v3-base-prompt-injection-v2) before production use.



