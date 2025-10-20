# Docker Compose - Ready to Use!

## ✅ Fixed Issues

The following issues have been resolved:

1. ✅ **Models not found error** - Fixed by baking models into image
2. ✅ **Config not found error** - Fixed by copying `config/` to runtime stage
3. ✅ **Environment variables** - Updated to use new configuration system
4. ✅ **Volume mount conflicts** - Removed conflicting volume mounts

## 🚀 Quick Start

### Just Run It!

```bash
# Build images and start services
docker-compose up --build

# Or in detached mode
docker-compose up -d --build
```

That's it! The models will be automatically downloaded during the build if they don't exist.

## 🏗️ What Happens During Build

### Automatic Model Download

```
1. Docker build starts
   ↓
2. Copies existing models from ./models (if any)
   ↓
3. Checks which models are missing
   ↓
4. Downloads missing models from HuggingFace
   ↓
5. Converts to ONNX format
   ↓
6. Includes models in final image
   ↓
7. Image is ready with all models!
```

### Build Process

**First build (no local models):**
```bash
$ docker-compose build

Step 1/20 : FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
...
Step 10/20 : COPY models/ /models/
 ---> No models found in repository
Step 12/20 : RUN python scripts/download_missing_models.py
 ---> Installing Python dependencies...
 ---> [DOWNLOAD] DeBERTa Prompt Injection: Missing
 ---> [DOWNLOAD] GraphCodeBERT Solidity Vulnerability: Missing
 ---> Downloading models from HuggingFace...
 ---> [OK] Successful: 2/2
...
Successfully built abc123def456
```

**Second build (with local models):**
```bash
$ docker-compose build

Step 10/20 : COPY models/ /models/
 ---> Copying local models
Step 12/20 : RUN python scripts/download_missing_models.py
 ---> [OK] All models present, skipping downloads
 ---> Build time: ~30 seconds (fast!)
...
Successfully built xyz789abc123
```

## 📋 Configuration

### docker-compose.yml (Production)

```yaml
promptguard-api:
  build:
    context: .
    dockerfile: Dockerfile
    args:
      - DOWNLOAD_MODELS=true  ← Models downloaded during build
  environment:
    - ModelsConfigPath=/app/config/models.json
    - TokenizerServiceUrl=http://tokenizer-service:8000
  # No volume mounts - models in image
```

**When to use:**
- ✅ Production deployments
- ✅ Portable images with models included
- ✅ Offline environments

### docker-compose.dev.yml (Development)

```yaml
promptguard-api:
  build:
    context: .
    dockerfile: Dockerfile
    args:
      - DOWNLOAD_MODELS=true  ← Models downloaded during build
  environment:
    - ModelsConfigPath=/app/config/models.json
  # Optional: Mount for live changes
  # volumes:
  #   - ./config:/app/config:ro
```

**When to use:**
- ✅ Development with containerized services
- ✅ Testing Docker builds locally
- ✅ CI/CD pipelines

## 🔄 Different Scenarios

### Scenario 1: Clean Build (No Local Models)

```bash
# Models will be downloaded during build
docker-compose up --build

# Build time: ~5-10 minutes (first time)
# Image size: ~1.2 GB (with models)
```

### Scenario 2: Build with Local Models

```bash
# Download models locally first (faster subsequent builds)
python scripts/download_missing_models.py --models-dir ./models

# Build will use local models, skip download
docker-compose up --build

# Build time: ~30 seconds (fast!)
# Image size: ~1.2 GB (with models)
```

### Scenario 3: Development with Volume Mounts

```bash
# Download models locally
python scripts/download_missing_models.py --models-dir ./models

# Edit docker-compose.yml to uncomment volume mounts
# volumes:
#   - ./models:/app/models:ro

# Run
docker-compose up --build

# Build time: ~30 seconds
# Can update models without rebuilding!
```

## 📊 Build Time Comparison

| Scenario | First Build | Rebuild | Model Updates |
|----------|-------------|---------|---------------|
| **Baked-in (current)** | 5-10 min | 30 sec | Rebuild needed |
| **Volume mount** | 30 sec | 30 sec | Just replace files |
| **Hybrid** | 2-3 min | 30 sec | Rebuild or replace |

## 🎯 Current Setup

### What's Configured

✅ **Dockerfile:**
- Copies local models if available
- Downloads missing models automatically
- Includes `config/` directory in runtime image
- Sets environment variables correctly

✅ **docker-compose.yml:**
- Builds with `DOWNLOAD_MODELS=true`
- No volume mounts (models in image)
- Correct environment variables
- Both services configured

✅ **config/models.json:**
- Defines both models
- Used by both Python and .NET
- Single source of truth

## 🧪 Testing

### Verify Everything Works

```bash
# 1. Build and start
docker-compose up --build

# 2. Wait for services to be healthy (~60 seconds)

# 3. Test API
curl http://localhost:5158/api/models

# Expected output:
{
  "count": 2,
  "models": [
    {
      "id": "deberta-v3-base-prompt-injection-v2",
      "name": "DeBERTa Prompt Injection",
      ...
    },
    {
      "id": "graphcodebert-solidity-vulnerability",
      "name": "GraphCodeBERT Solidity Vulnerability Detector",
      ...
    }
  ]
}
```

### Test Model Endpoints

```bash
# Test DeBERTa model
curl -X POST http://localhost:5158/api/models/deberta-v3-base-prompt-injection-v2/detect \
  -H "Content-Type: application/json" \
  -d '{"text":"Ignore previous instructions"}'

# Test GraphCodeBERT model
curl -X POST http://localhost:5158/api/models/graphcodebert-solidity-vulnerability/detect \
  -H "Content-Type: application/json" \
  -d '{"text":"contract Vulnerable { ... }"}'
```

## 🐛 Troubleshooting

### Models still not found?

**Check 1: Config file in image**
```bash
docker run --rm inference-service cat /app/config/models.json
```

**Check 2: Models in image**
```bash
docker run --rm inference-service ls -la /app/models/
```

**Check 3: Environment variables**
```bash
docker run --rm inference-service env | grep Models
```

### Build fails during model download?

**Solution 1: Use local models**
```bash
# Download models locally first
python scripts/download_missing_models.py --models-dir ./models

# Build will skip download (faster)
docker-compose build
```

**Solution 2: Check internet connection**
- Model download requires internet to access HuggingFace
- Check if firewall is blocking downloads

**Solution 3: Use pre-built image**
```bash
# Build with models once
docker build -f Dockerfile.with-models -t myimage:with-models .

# Update docker-compose to use pre-built image
docker-compose up
```

## 📝 Summary of Changes

### Files Updated

1. **`Dockerfile`**
   - ✅ Copies `config/` to runtime stage
   - ✅ Updated environment variables
   - ✅ Smart model downloading

2. **`Dockerfile.with-models`**
   - ✅ Copies `config/` to runtime stage
   - ✅ Updated environment variables
   - ✅ Always downloads all models

3. **`docker-compose.yml`**
   - ✅ Sets `DOWNLOAD_MODELS=true`
   - ✅ Removed volume mounts
   - ✅ Updated environment variables

4. **`docker-compose.dev.yml`**
   - ✅ Sets `DOWNLOAD_MODELS=true`
   - ✅ Optional volume mounts for development
   - ✅ Updated environment variables

5. **`appsettings.json`**
   - ✅ Removed duplicate model config
   - ✅ Points to `config/models.json`

6. **`appsettings.Development.json`**
   - ✅ Removed duplicate model config
   - ✅ Points to `config/models.json`

## 🎉 Result

**You can now run `docker-compose up` and it will:**

1. ✅ Build the images
2. ✅ Download models automatically if missing
3. ✅ Include models in the image
4. ✅ Start services with models ready
5. ✅ Work without any volume mounts
6. ✅ Be fully portable

**Just run:** `docker-compose up --build` 🚀

No manual model downloads, no volume mount issues, no configuration errors!


