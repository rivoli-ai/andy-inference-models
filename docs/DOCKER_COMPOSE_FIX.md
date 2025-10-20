# Docker Compose Model Loading Fix

## 🔴 Problem

When running `docker-compose up`, you get:
```
Model file not found at: models/deberta-v3-base-prompt-injection-v2/model.onnx
```

## 🔍 Root Cause

The `docker-compose.yml` file mounts `./models:/app/models`, but your local `./models` directory is **empty**!

```yaml
volumes:
  - ./models:/app/models:ro  ← Mounting empty directory!
```

This overrides the models that were copied into the Docker image during build.

## ✅ Solutions

You have **3 options** to fix this:

### Option 1: Download Models Locally (Recommended for Development)

Download models to your local `./models` directory:

```bash
# Download models using the script
python scripts/download_missing_models.py --models-dir ./models

# Verify models were downloaded
ls models/deberta-v3-base-prompt-injection-v2/
ls models/graphcodebert-solidity-vulnerability/

# Now run docker-compose
docker-compose up
```

**Pros:**
- ✅ Fast rebuilds (models not in image)
- ✅ Can update models without rebuilding
- ✅ Smaller image size

**Cons:**
- ❌ Requires local model download
- ❌ Models not portable with image

---

### Option 2: Use Dockerfile.with-models (Recommended for Production)

Build with models baked into the image:

```bash
# Update docker-compose.yml to use Dockerfile.with-models
```

Edit `docker-compose.yml`:
```yaml
promptguard-api:
  build:
    context: .
    dockerfile: Dockerfile.with-models  ← Change this
  volumes:
    # Remove or comment out models volume
    # - ./models:/app/models:ro
```

Then run:
```bash
docker-compose build
docker-compose up
```

**Pros:**
- ✅ Models baked into image (portable)
- ✅ No local models needed
- ✅ Works offline after build

**Cons:**
- ❌ Slower initial build (~2-3 minutes for download)
- ❌ Larger image size
- ❌ Rebuild needed to update models

---

### Option 3: Remove Volume Mount (Quick Fix)

Simply don't mount the models directory:

Edit `docker-compose.yml`:
```yaml
volumes:
  # Comment out the models mount to use models from image
  # - ./models:/app/models:ro
```

Then rebuild:
```bash
docker-compose build --build-arg DOWNLOAD_MODELS=true
docker-compose up
```

**Pros:**
- ✅ Quick fix
- ✅ Uses models from image

**Cons:**
- ❌ Need to rebuild to update models

---

## 🚀 Recommended Approach

### For Development
```bash
# 1. Download models locally once
python scripts/download_missing_models.py --models-dir ./models

# 2. Use docker-compose with volume mount
docker-compose -f docker-compose.dev.yml up
```

### For Production
```bash
# Use Dockerfile.with-models with models baked in
docker-compose -f docker-compose.yml up
# (after updating to use Dockerfile.with-models)
```

## 🔧 Updated docker-compose.yml

Here's a complete updated version that handles both scenarios:

```yaml
version: '3.8'

services:
  tokenizer-service:
    build:
      context: ./tokenizer-service
      dockerfile: Dockerfile
    container_name: tokenizer-service
    ports:
      - "8000:8000"
    volumes:
      - ./models:/app/models:ro
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "python", "-c", "import requests; requests.get('http://localhost:8000/health')"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
    networks:
      - promptguard-network

  promptguard-api:
    build:
      context: .
      dockerfile: Dockerfile.with-models  # ← Use this to bake models in
      # Or use Dockerfile + local models
    container_name: inference-service
    ports:
      - "5158:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8080
      - Logging__MaxLogSize=1000
      - ModelsConfigPath=/app/config/models.json
      - TokenizerServiceUrl=http://tokenizer-service:8000
    volumes:
      # Option A: Mount local models (if you downloaded them)
      - ./models:/app/models:ro
      
      # Option B: Don't mount (use models baked in image)
      # Comment out the line above if using Dockerfile.with-models
      
      # Always mount config for easy updates
      - ./config:/app/config:ro
    restart: unless-stopped
    depends_on:
      tokenizer-service:
        condition: service_healthy
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/api/models"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 60s
    networks:
      - promptguard-network

networks:
  promptguard-network:
    driver: bridge
```

## 🎯 Quick Fix Steps

### Immediate Solution (Choose One)

**A. Download Models Locally:**
```bash
# Install dependencies
pip install torch transformers optimum[onnxruntime] onnx

# Download models
python scripts/download_missing_models.py --models-dir ./models

# Run docker-compose
docker-compose up
```

**B. Use Image with Baked-In Models:**
```bash
# Update docker-compose.yml to use Dockerfile.with-models
# Comment out the models volume mount

# Rebuild
docker-compose build

# Run
docker-compose up
```

## 🔍 Verify Models

After downloading or building, verify models exist:

```bash
# Check local models
ls -la models/deberta-v3-base-prompt-injection-v2/
ls -la models/graphcodebert-solidity-vulnerability/

# Or check in container
docker run --rm inference-service ls -la /app/models/
```

## ⚠️ Common Issues

### Issue 1: Volume Mount Overrides Image

**Problem:**
```yaml
volumes:
  - ./models:/app/models:ro  ← Overrides models in image!
```

**Solution:**
- If using local models: Ensure `./models` is populated
- If using image models: Comment out the volume mount

### Issue 2: Models Not in Image

**Problem:** Built with `DOWNLOAD_MODELS=false` but no local models

**Solution:**
```bash
# Build with model download
docker-compose build --build-arg DOWNLOAD_MODELS=true

# Or use Dockerfile.with-models
```

### Issue 3: .dockerignore Blocking Models

**Problem:** `**/models/*` in `.dockerignore`

**Solution:** Already fixed! Line 39 is commented out:
```dockerignore
# **/models/*  ← Commented out
```

## 📝 Best Practices

### Development Workflow
```bash
# 1. Download models once
python scripts/download_missing_models.py --models-dir ./models

# 2. Use volume mounts for fast iteration
docker-compose -f docker-compose.dev.yml up

# 3. Update models locally without rebuilding
python scripts/download_missing_models.py --force --models-dir ./models
docker-compose restart
```

### Production Workflow
```bash
# 1. Build with models baked in
docker build -f Dockerfile.with-models -t myimage .

# 2. No volume mounts needed
docker run -p 8080:8080 myimage

# Or use docker-compose with Dockerfile.with-models
```

## ✅ Summary

**The error occurs because:**
- ❌ Local `./models` directory is empty
- ❌ Volume mount overrides image models
- ❌ No models available at runtime

**Fix by:**
- ✅ Downloading models locally, OR
- ✅ Using `Dockerfile.with-models`, OR
- ✅ Removing volume mount and building with models

**Choose the approach that fits your workflow!** 🚀


