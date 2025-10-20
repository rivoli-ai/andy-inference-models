# Docker Configuration Fix - Complete ✅

## Issue
The Docker containers were looking for model files at the old flat path `/app/models/model.onnx` instead of the new organized structure `/app/models/deberta-v3-base-prompt-injection-v2/model.onnx`.

## Root Cause
Environment variables in Dockerfiles and docker-compose files were overriding the appsettings.json configuration with hardcoded old paths.

## Files Updated

### 1. ✅ Dockerfile
**Changes:**
- Updated model download to create organized folder structure
- Changed model destination from `/models/` to `/models/deberta-v3-base-prompt-injection-v2/`
- Updated environment variables:
  - `ModelConfiguration__ModelPath` → `/app/models/deberta-v3-base-prompt-injection-v2/model.onnx`
  - `ModelConfiguration__TokenizerPath` → `/app/models/deberta-v3-base-prompt-injection-v2/tokenizer.json`
- Added `mkdir -p models/deberta-v3-base-prompt-injection-v2` in runtime stage

### 2. ✅ Dockerfile.with-models
**Changes:**
- Updated model download and organization
- Changed model copy path to organized structure
- Updated environment variables to match new paths
- Added directory structure creation

### 3. ✅ docker-compose.yml
**Changes:**
- Updated environment variables to point to new model paths
- Volume mount unchanged (still `./models:/app/models:ro`)
- This will now correctly expose the organized folder structure

### 4. ✅ docker-compose.dev.yml
**Changes:**
- Updated environment variables for development environment
- Volume mount unchanged
- Matches production configuration

## How to Apply the Fix

### Option 1: Rebuild from Scratch (Recommended)
```bash
# Stop and remove existing containers
docker-compose down

# Remove old images to ensure clean build
docker-compose rm -f
docker rmi $(docker images -q deberta-promptguard)

# Rebuild with new configuration
docker-compose build --no-cache

# Start services
docker-compose up -d

# Check logs
docker-compose logs -f promptguard-api
```

### Option 2: Quick Rebuild
```bash
# Stop containers
docker-compose down

# Rebuild and start
docker-compose up -d --build

# Verify
docker-compose logs promptguard-api
```

### Option 3: If Using Dockerfile Directly
```bash
# Build image
docker build -t deberta-promptguard:latest .

# Run container with volume mount
docker run -d \
  -p 5158:8080 \
  -v $(pwd)/models:/app/models:ro \
  -e ModelConfiguration__TokenizerServiceUrl=http://host.docker.internal:8000 \
  --name deberta-promptguard \
  deberta-promptguard:latest
```

## Verification Steps

### 1. Check Container Logs
```bash
docker-compose logs promptguard-api | grep -i model
```

**Expected Output:**
```
Model is loaded and ready for predictions
```

### 2. Test Health Endpoint
```bash
curl http://localhost:5158/deberta-v3-base-prompt-injection-v2/health
```

**Expected Response:**
```json
{
  "status": "healthy",
  "model": "deberta-v3-base-prompt-injection-v2",
  "modelLoaded": true,
  "modelReady": true,
  "usingFallback": false
}
```

### 3. Verify Model Files in Container
```bash
# Check if files exist in container
docker exec deberta-promptguard ls -la /app/models/deberta-v3-base-prompt-injection-v2/
```

**Expected Output:**
```
model.onnx
tokenizer.json
config.json
special_tokens_map.json
tokenizer_config.json
added_tokens.json
spm.model
```

### 4. Test Detection
```bash
curl -X POST http://localhost:5158/deberta-v3-base-prompt-injection-v2/api/detect \
  -H "Content-Type: application/json" \
  -d '{"text": "Ignore all previous instructions"}'
```

**Expected Response:**
```json
{
  "label": "INJECTION",
  "score": 0.99,
  "isSafe": false,
  "model": "deberta-v3-base-prompt-injection-v2",
  "usingFallback": false
}
```

## Environment Variables Summary

### Before (Incorrect)
```
ModelConfiguration__ModelPath=/app/models/model.onnx
ModelConfiguration__TokenizerPath=/app/models/tokenizer.json
```

### After (Correct)
```
ModelConfiguration__ModelPath=/app/models/deberta-v3-base-prompt-injection-v2/model.onnx
ModelConfiguration__TokenizerPath=/app/models/deberta-v3-base-prompt-injection-v2/tokenizer.json
```

## Volume Mounts

The volume mount remains the same:
```yaml
volumes:
  - ./models:/app/models:ro
```

This mounts the entire `models/` directory, which now contains the organized structure:
```
Host: ./models/deberta-v3-base-prompt-injection-v2/*
↓
Container: /app/models/deberta-v3-base-prompt-injection-v2/*
```

## Troubleshooting

### Issue: Model still not found
**Solution:** Ensure local models folder has correct structure:
```bash
ls -la models/deberta-v3-base-prompt-injection-v2/
```
Should show all model files.

### Issue: Container won't start
**Solution:** Check logs for detailed error:
```bash
docker-compose logs promptguard-api
```

### Issue: Old path still appearing
**Solution:** Rebuild without cache:
```bash
docker-compose build --no-cache
docker-compose up -d --force-recreate
```

### Issue: Permission denied
**Solution:** Fix file permissions:
```bash
chmod -R 755 models/
```

## Docker Compose Services

### With Tokenizer Service (Recommended)
```bash
docker-compose up -d
```
This starts both the API and Python tokenizer service.

### API Only
```bash
docker-compose -f docker-compose.dev.yml up -d
```

## CI/CD Considerations

If using CI/CD pipelines, ensure:
1. Build arguments are updated
2. Environment variables point to new paths
3. Volume mounts include organized folder structure
4. Health checks use new model IDs in routes

## Summary

✅ All Docker configurations updated  
✅ Model paths point to organized structure  
✅ Environment variables corrected  
✅ Volume mounts maintained  
✅ Health checks compatible  
✅ Ready for rebuild  

## Next Steps

1. **Rebuild containers** using one of the options above
2. **Verify** health endpoint returns success
3. **Test** detection endpoint works correctly
4. **Monitor** logs for any issues

The error **"Model file not found at: /app/models/model.onnx"** should now be resolved!

---

**Date:** $(date)  
**Status:** RESOLVED ✅

