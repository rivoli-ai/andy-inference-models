# Docker Build & Run Guide - Model Loading Fix

## 🔴 Problem

Running `docker-compose up` shows:
```
Model file not found at: models/deberta-v3-base-prompt-injection-v2/model.onnx
```

Even though `DOWNLOAD_MODELS=true` is set.

## 🔍 Root Causes

1. **Docker Build Cache** - Old build without models is cached
2. **Local models directory empty** - If mounted as volume, overrides image models
3. **Build didn't complete** - Model download may have failed silently

## ✅ Complete Fix - Clean Rebuild

### Step 1: Clean Everything

```bash
# Stop and remove all containers
docker-compose down

# Remove the old images (force clean build)
docker rmi inference-service tokenizer-service

# Or remove all related images
docker images | grep inference
docker images | grep tokenizer
# Then manually remove with: docker rmi <image-id>

# Optionally clear build cache
docker builder prune -f
```

### Step 2: Verify Local Setup

```bash
# Check if models directory exists
ls -la models/

# If it's empty or doesn't exist, that's OK!
# The Docker build will download models

# Verify config file exists
cat config/models.json
```

### Step 3: Clean Build

```bash
# Build without cache to ensure models are downloaded
docker-compose build --no-cache --build-arg DOWNLOAD_MODELS=true

# This will:
# 1. Build from scratch
# 2. Download models from HuggingFace (~5-10 min)
# 3. Include models in the image
```

### Step 4: Verify Build Succeeded

Check build output for these messages:

```
✓ Expected output during build:
...
Step X: Downloading missing models...
Installing Python dependencies for model conversion...
[DOWNLOAD] DeBERTa Prompt Injection: Missing
[DOWNLOAD] GraphCodeBERT Solidity Vulnerability: Missing
...
[OK] DeBERTa Prompt Injection: Download successful!
[OK] GraphCodeBERT Solidity Vulnerability: Download successful!
...
=== Final Model Status ===
/models/deberta-v3-base-prompt-injection-v2/model.onnx
/models/graphcodebert-solidity-vulnerability/model.onnx
```

### Step 5: Start Services

```bash
# Start the services
docker-compose up

# Or in detached mode
docker-compose up -d

# Watch logs
docker-compose logs -f
```

### Step 6: Verify Models Are Loaded

```bash
# Check API models endpoint
curl http://localhost:5158/api/models

# Should return both models:
{
  "count": 2,
  "models": [...]
}

# Check if models exist in container
docker exec inference-service ls -la /app/models/

# Should show:
# deberta-v3-base-prompt-injection-v2/
# graphcodebert-solidity-vulnerability/
```

## 🚀 Quick Fix Command

If you just want to fix it quickly:

```bash
# One command to clean, rebuild, and start
docker-compose down && \
docker-compose build --no-cache && \
docker-compose up
```

## 🔍 Troubleshooting

### Issue 1: Build Fails During Model Download

**Error:**
```
Failed to download model from HuggingFace
```

**Solution:**
```bash
# Check internet connection
ping huggingface.co

# Try building with verbose output
docker-compose build --no-cache --progress=plain

# Or download models locally first
python scripts/download_missing_models.py --models-dir ./models

# Then build with local models
docker-compose build
```

### Issue 2: Build Succeeds But Models Still Missing

**Check if models are in the image:**
```bash
# Inspect the image
docker run --rm inference-service ls -la /app/models/

# Should show both model directories with files
```

**If empty:**
- Check `.dockerignore` - ensure `**/models/*` is commented out
- Verify `COPY --from=model-downloader /models/ ./models/` is in Dockerfile
- Rebuild with `--no-cache`

### Issue 3: Volume Mount Overriding

**If you uncommented volumes:**
```yaml
volumes:
  - ./models:/app/models:ro  ← This overrides image models!
```

**Solution:**
- Either populate `./models` locally, OR
- Comment out the volume mount

### Issue 4: Config File Not Found

**Error:**
```
Configuration file not found: config/models.json
```

**Solution:**
```bash
# Verify config exists
ls -la config/models.json

# Check if it's copied to image
docker run --rm inference-service cat /app/config/models.json

# If not found, rebuild
docker-compose build --no-cache
```

## 📋 Verification Checklist

Before running docker-compose:

- ✅ `config/models.json` exists and is valid JSON
- ✅ `.dockerignore` has `**/models/*` commented out (line 39)
- ✅ `Dockerfile` has `COPY config/ ./config/` (line 69)
- ✅ `Dockerfile` has `COPY --from=model-downloader /models/ ./models/` (line 72)
- ✅ No volume mounts for `./models` in docker-compose.yml (or local models exist)

## 🎯 Recommended Workflow

### For Clean Builds (First Time)

```bash
# 1. Clean rebuild
docker-compose build --no-cache --build-arg DOWNLOAD_MODELS=true

# 2. Start services
docker-compose up -d

# 3. Verify
curl http://localhost:5158/api/models
```

### For Subsequent Builds (With Cache)

```bash
# Models are already in image cache, fast build
docker-compose build

# Start
docker-compose up -d
```

### For Development (With Local Models)

```bash
# 1. Download models locally
python scripts/download_missing_models.py --models-dir ./models

# 2. Uncomment volume mount in docker-compose.yml
# volumes:
#   - ./models:/app/models:ro

# 3. Build and run
docker-compose up --build
```

## 🐳 Docker Compose Configuration Status

### Current Setup (Correct)

```yaml
promptguard-api:
  build:
    args:
      - DOWNLOAD_MODELS=true  ✅ Models will download during build
  # volumes: (commented out)  ✅ Won't override image models
  #   - ./models:/app/models:ro
```

**This should work!** If it doesn't, you need a clean rebuild.

## 🔧 Clean Rebuild Script

Create a file `rebuild.sh`:

```bash
#!/bin/bash
echo "Cleaning up old containers and images..."
docker-compose down
docker rmi inference-service tokenizer-service 2>/dev/null || true

echo "Building fresh images with models..."
docker-compose build --no-cache --build-arg DOWNLOAD_MODELS=true

echo "Starting services..."
docker-compose up -d

echo "Waiting for services to be healthy..."
sleep 10

echo "Checking models..."
curl http://localhost:5158/api/models

echo "Done!"
```

Run it:
```bash
chmod +x rebuild.sh
./rebuild.sh
```

## ✅ Expected Behavior

When you run `docker-compose up --build`:

1. **Build starts** → Builds .NET app
2. **Model stage** → Checks for models
3. **Download** → Downloads missing models (~5-10 min first time)
4. **Include** → Copies models to runtime image
5. **Start** → Services start with models included
6. **API Ready** → Models available at `/api/models`

**No local models directory needed!**

## 🎉 Summary

**To fix the "Model file not found" error:**

```bash
# Clean rebuild (one command)
docker-compose down && \
docker-compose build --no-cache && \
docker-compose up

# Wait ~5-10 minutes for model download on first build
# Subsequent builds will be fast (~30 seconds)
```

**The models should automatically:**
- ✅ Download during Docker build
- ✅ Be included in the image
- ✅ Be available when container starts
- ✅ Work without local models directory

**If still not working, check the troubleshooting section above!** 🚀


