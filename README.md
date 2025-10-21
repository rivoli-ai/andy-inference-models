# AI Inference Models API

A production-ready .NET solution for running multiple AI models with unified REST API. Supports DeBERTa for prompt injection detection and GraphCodeBERT for smart contract vulnerability analysis.

## Overview

This solution provides a high-performance REST API for AI model inference using ONNX Runtime. It supports multiple models simultaneously with accurate HuggingFace tokenization via a Python microservice.

**Supported Models:**
- **DeBERTa v3** - Prompt injection detection (SAFE vs INJECTION)
- **GraphCodeBERT** - Solidity vulnerability detection (SAFE vs VULNERABLE)
- **Extensible** - Easy to add new models via JSON configuration

## Architecture

- **InferenceModel.ML**: Core ML library with ONNX Runtime integration and tokenization
- **InferenceModel.Api**: ASP.NET Core 8.0 Web API with multi-model support
- **Tokenizer Service**: Python FastAPI microservice for accurate HuggingFace tokenization

## Prerequisites

**For Docker (Recommended):**
- Docker and Docker Compose installed
- That's it! Models download automatically

**For Local Development:**
- .NET 8.0 SDK
- Python 3.11+ (for tokenizer service)
- Model files (auto-downloaded or manual)

## 🐳 Docker Quick Start (Recommended)

Get running in under 5 minutes with Docker Compose:

```bash
# Enable BuildKit for faster builds (optional but recommended)
export DOCKER_BUILDKIT=1              # Linux/Mac
$env:DOCKER_BUILDKIT=1                # Windows PowerShell

# Start both API and tokenizer services
docker-compose up --build

# Or run in background
docker-compose up --build -d
```

**That's it!** Access the API at:
- **Swagger UI:** http://localhost:5158/swagger
- **Health Check:** http://localhost:5158/health
- **Available Models:** http://localhost:5158/api/models

**Documentation:** (Numbered in recommended reading order)

**🚀 Getting Started:**
1. **[Quick Start](docs/01-QUICK_START.md)** - Get running in 5 minutes
2. **[Docker Compose Usage](docs/02-DOCKER_COMPOSE_USAGE.md)** - Which compose file to use?
3. **[Docker Quickstart](docs/03-DOCKER_QUICKSTART.md)** - One-page Docker reference

**🐳 Docker Guides:**
4. **[Docker Compose Guide](docs/05-DOCKER_COMPOSE_GUIDE.md)** - Complete compose comparison
5. **[Docker Build Guide](docs/06-DOCKER_BUILD_GUIDE.md)** - Build optimization
6. **[Docker Run Guide](docs/07-DOCKER_RUN.md)** - Docker run details
7. **[Docker Advanced](docs/08-DOCKER_ADVANCED.md)** - Advanced Docker topics

**🔧 Advanced Topics:**
8. **[Tokenizer Service Setup](docs/09-TOKENIZER_SERVICE_SETUP.md)** - Python tokenizer
9. **[Tokenizer Implementation](docs/10-TOKENIZER_IMPLEMENTATION_GUIDE.md)** - Implementation details
10. **[Model Conversion](docs/11-MODEL_CONVERSION.md)** - Converting models to ONNX
11. **[Architecture](docs/12-PROJECT_ARCHITECTURE.md)** - System architecture
12. **[Tokenizer Notes](docs/13-TOKENIZER_TECHNICAL_NOTES.md)** - Technical details

## API Endpoints

### Core Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/health` | GET | Basic health check |
| `/api/models` | GET | List all available models |
| `/api/models/{model}/predict` | POST | Single prediction |
| `/api/predict/batch` | POST | Batch prediction (mixed models) |
| `/api/models/{model}/health` | GET | Model-specific health |
| `/api/logs` | GET | View prediction logs |
| `/api/logs/stats` | GET | Prediction statistics |

### Example: Single Prediction

```bash
curl -X POST http://localhost:5158/api/models/deberta-v3-base-prompt-injection-v2/predict \
  -H "Content-Type: application/json" \
  -d '{"text": "Ignore previous instructions"}'
```

**Response:**
```json
{
  "label": "INJECTION",
  "score": 0.9998,
  "isSafe": false,
  "text": "Ignore previous instructions",
  "model": "deberta-v3-base-prompt-injection-v2",
  "predictionMethod": "ml-model"
}
```

### Example: Batch Prediction (Mixed Models)

```bash
curl -X POST http://localhost:5158/api/predict/batch \
  -H "Content-Type: application/json" \
  -d '[
    {"text": "Ignore instructions", "model": "deberta-v3-base-prompt-injection-v2"},
    {"text": "function transfer(address to, uint amount) { balance[to] += amount; }", "model": "graphcodebert-solidity-vulnerability"}
  ]'
```

Each item in the batch can use a different model!

## Features

✅ **Multi-Model Support** - Run multiple AI models simultaneously  
✅ **Accurate Tokenization** - Python microservice with HuggingFace tokenizers  
✅ **Fast Inference** - ONNX Runtime with CPU optimization  
✅ **Production Ready** - Health checks, logging, graceful fallbacks  
✅ **Easy Configuration** - JSON-based model configuration  
✅ **Docker First** - Optimized containers with 99% faster rebuilds  
✅ **REST API** - OpenAPI/Swagger documentation  
✅ **Batch Processing** - Process multiple texts with mixed models  

## Performance

| Metric | Value |
|--------|-------|
| **Inference Time** | 50-200ms per request (CPU) |
| **Max Sequence Length** | 512 tokens |
| **Concurrent Requests** | Handled via ASP.NET Core async |
| **Model Loading** | Singleton pattern (loaded once) |
| **Docker Rebuild** | 3-5 seconds with BuildKit cache |

## Adding New Models

Add any HuggingFace sequence classification model by editing `config/models.json`:

```json
{
  "id": "your-model-id",
  "name": "Your Model Name",
  "huggingface_model": "author/model-name",
  "output_dir": "model-directory",
  "labels": ["LABEL1", "LABEL2"],
  "description": "Model description"
}
```

See [docs/11-MODEL_CONVERSION.md](docs/11-MODEL_CONVERSION.md) for details.

## Technology Stack

- **Backend**: .NET 8.0, ASP.NET Core, ONNX Runtime
- **Tokenizer**: Python 3.11, FastAPI, HuggingFace Transformers
- **ML Format**: ONNX (Open Neural Network Exchange)
- **Container**: Docker, Docker Compose
- **API**: REST with OpenAPI/Swagger

## License

This project uses AI models from HuggingFace which have their own licenses. Please check each model's license before production use:
- [DeBERTa v3 Prompt Injection](https://huggingface.co/protectai/deberta-v3-base-prompt-injection-v2)
- [GraphCodeBERT Vulnerability](https://huggingface.co/angusleung100/GraphCodeBERT-Base-Solidity-Vulnerability)

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

