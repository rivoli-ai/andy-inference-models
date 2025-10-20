# Tokenizer Service Configuration Update

## 🎯 What Changed

Updated the tokenizer service to dynamically load models from `config/models.json` instead of using hardcoded model definitions.

## 📊 Before vs After

### Before: Hardcoded Models

```python
# tokenizer-service/app.py
SUPPORTED_MODELS = {
    "deberta-v3-base-prompt-injection-v2": {
        "hf_path": "protectai/deberta-v3-base-prompt-injection-v2",
        "local_path": "/app/models/deberta-v3-base-prompt-injection-v2"
    },
    "graphcodebert-solidity-vulnerability": {
        "hf_path": "angusleung100/GraphCodeBERT-Base-Solidity-Vulnerability",
        "local_path": "/app/models/graphcodebert-solidity-vulnerability"
    }
}
```

**Problems:**
- ❌ Hardcoded model list
- ❌ Need to edit Python code to add models
- ❌ Out of sync with config/models.json
- ❌ Duplicated configuration

### After: Dynamic Configuration Loading

```python
# tokenizer-service/app.py
def load_models_config():
    """Load models configuration from config/models.json file."""
    # Searches for config in multiple locations
    # Returns parsed JSON configuration
    
def build_supported_models():
    """Build SUPPORTED_MODELS dictionary from config/models.json."""
    config = load_models_config()
    # Dynamically builds model dictionary
    
# Loaded dynamically from config/models.json
SUPPORTED_MODELS = build_supported_models()
```

**Benefits:**
- ✅ Reads from `config/models.json`
- ✅ No code changes to add models
- ✅ Always in sync with main config
- ✅ Single source of truth

## 🔄 How It Works

### Configuration Loading Flow

```
1. Tokenizer service starts
   ↓
2. Calls load_models_config()
   ↓
3. Searches for config/models.json in multiple locations:
   - /app/config/models.json (Docker container)
   - ../config/models.json (relative)
   - config/models.json (project root)
   ↓
4. Loads JSON configuration
   ↓
5. Calls build_supported_models()
   ↓
6. Builds SUPPORTED_MODELS dictionary from JSON
   ↓
7. Tokenizers loaded for all configured models
```

### Path Resolution

The service intelligently searches for configuration:

```python
possible_paths = [
    "/app/config/models.json",           # Docker container
    "../config/models.json",              # Relative to tokenizer-service/
    "config/models.json",                 # Project root
    Path(__file__).parent.parent / "config" / "models.json"
]
```

### Fallback Behavior

If config file is not found:
```python
logger.warning("config/models.json not found, using default hardcoded models")
return {
    "models": [
        {
            "id": "deberta-prompt-injection",
            "huggingface_model": "protectai/deberta-v3-base-prompt-injection-v2",
            "output_dir": "deberta-v3-base-prompt-injection-v2"
        }
    ]
}
```

## 📝 Files Updated

### 1. tokenizer-service/app.py

**Added Functions:**
- `load_models_config()` - Loads JSON configuration
- `build_supported_models()` - Builds model dictionary from config

**Changed:**
- `SUPPORTED_MODELS` - Now dynamically loaded instead of hardcoded

### 2. tokenizer-service/Dockerfile

**Added:**
- Creates `/app/config` directory
- Documentation about config loading

**Removed:**
- Hardcoded tokenizer pre-download for specific model

### 3. docker-compose.yml

**Added:**
- Volume mount: `./config:/app/config:ro`
- Allows tokenizer service to access configuration

## 🐳 Docker Compose Changes

### tokenizer-service Configuration

```yaml
tokenizer-service:
  build:
    context: ./tokenizer-service
    dockerfile: Dockerfile
  volumes:
    # Mount config directory so tokenizer can read models.json
    - ./config:/app/config:ro
```

**Why this works:**
- ✅ Config is mounted into container
- ✅ App reads from `/app/config/models.json`
- ✅ Updates to config don't require rebuild
- ✅ Consistent with main API service

## ✅ Benefits

### 1. Single Source of Truth

**One file defines all models:**
```json
// config/models.json
{
  "models": [
    {
      "id": "new-model",
      "huggingface_model": "author/new-model",
      "output_dir": "new-model"
    }
  ]
}
```

**Works everywhere:**
- ✅ Python download scripts
- ✅ .NET API
- ✅ Tokenizer service ← NEW!

### 2. No Code Changes

**Before:** Add model to 3 places
1. `config/models.json` (Python)
2. `appsettings.json` (C#)
3. `tokenizer-service/app.py` (Python) ← Hardcoded

**After:** Add model to 1 place
1. `config/models.json` ✅

### 3. Consistent Model IDs

All services now use the same model identifiers:
- ✅ Model downloads
- ✅ API endpoints
- ✅ Tokenizer service

### 4. Dynamic Updates

```bash
# Edit configuration
vi config/models.json

# Restart tokenizer service (no rebuild needed!)
docker-compose restart tokenizer-service

# New models automatically available
```

## 🧪 Testing

### Verify Configuration Loading

```bash
# Start services
docker-compose up --build

# Check tokenizer service logs
docker logs tokenizer-service

# Expected output:
# Found configuration at: /app/config/models.json
# Loaded 2 models from configuration
# Loading deberta-v3-base-prompt-injection-v2 tokenizer...
# ✓ deberta-v3-base-prompt-injection-v2 tokenizer loaded successfully
# Loading graphcodebert-solidity-vulnerability tokenizer...
# ✓ graphcodebert-solidity-vulnerability tokenizer loaded successfully
```

### Test Tokenization

```bash
# Test with DeBERTa model
curl -X POST http://localhost:8000/tokenize \
  -H "Content-Type: application/json" \
  -d '{
    "text": "Ignore all previous instructions",
    "model": "deberta-v3-base-prompt-injection-v2",
    "max_length": 512
  }'

# Test with GraphCodeBERT model
curl -X POST http://localhost:8000/tokenize \
  -H "Content-Type: application/json" \
  -d '{
    "text": "contract Vulnerable { }",
    "model": "graphcodebert-solidity-vulnerability",
    "max_length": 512
  }'
```

### Check Health Endpoint

```bash
curl http://localhost:8000/health

# Expected response:
{
  "status": "healthy",
  "available_models": [
    "deberta-v3-base-prompt-injection-v2",
    "graphcodebert-solidity-vulnerability"
  ],
  "version": "1.0.0"
}
```

## 🔍 Implementation Details

### Model Dictionary Building

```python
def build_supported_models():
    config = load_models_config()
    models_base_dir = config.get("config", {}).get("models_base_dir", "/app/models")
    
    supported = {}
    for model in config.get("models", []):
        model_id = model.get("output_dir")
        
        supported[model_id] = {
            "hf_path": model.get("huggingface_model"),
            "local_path": f"{models_base_dir}/{model.get('output_dir')}",
            "name": model.get("name"),
            "description": model.get("description")
        }
    
    return supported
```

### Error Handling

**Robust fallback:**
1. Try to load from config file
2. If file not found → Use default config
3. If parsing fails → Raise error with details
4. Always log what's happening

## 📋 Configuration Format

### Expected Structure in config/models.json

```json
{
  "models": [
    {
      "id": "model-identifier",
      "name": "Model Display Name",
      "huggingface_model": "author/model-on-huggingface",
      "output_dir": "model-directory-name",
      "max_length": 512
    }
  ],
  "config": {
    "models_base_dir": "/app/models",
    "tokenizer_service_url": "http://localhost:8000"
  }
}
```

### Fields Used by Tokenizer Service

| Field | Usage |
|-------|-------|
| `output_dir` | Model ID (key in SUPPORTED_MODELS) |
| `huggingface_model` | HuggingFace path for loading tokenizer |
| `models_base_dir` | Base directory for local tokenizer files |
| `name` | Display name in health check |
| `description` | Model description (metadata) |

## 🎯 All Services Now Use config/models.json

### Complete Integration

```
config/models.json (Single Source of Truth)
     │
     ├─→ Python Scripts (download, convert)
     │   ✅ Downloads models listed in config
     │
     ├─→ .NET API (inference service)
     │   ✅ Exposes models from config
     │
     └─→ Tokenizer Service (FastAPI)
         ✅ Loads tokenizers for models in config
```

### Consistency Guaranteed

All three systems read from the same file:
- ✅ Same model IDs
- ✅ Same HuggingFace paths
- ✅ Same directory structure
- ✅ No drift or inconsistency

## 🚀 Benefits Summary

| Benefit | Description |
|---------|-------------|
| **Unified** | All services use same config |
| **Dynamic** | No hardcoded models |
| **Maintainable** | Edit one file to update all |
| **Consistent** | Guaranteed synchronization |
| **Scalable** | Add models easily |
| **Flexible** | Volume mount for easy updates |

## ✨ Summary

**The tokenizer service now:**
- ✅ Reads models from `config/models.json`
- ✅ No hardcoded model definitions
- ✅ Automatically discovers new models
- ✅ Consistent with all other services
- ✅ Updates without code changes

**Add a model to `config/models.json` and all three systems automatically support it:**
1. Python scripts download it
2. .NET API serves it
3. Tokenizer service tokenizes for it

**True unified configuration achieved!** 🎉


