# Quick Fix - Docker Model Path Error

## Problem
```
"modelError": "Model file not found at: /app/models/model.onnx"
```

## Solution
The Docker configuration has been updated to use the new organized folder structure.

## Apply Fix (Choose One)

### Option A: Full Rebuild (Recommended)
```bash
docker-compose down
docker-compose build --no-cache
docker-compose up -d
```

### Option B: Quick Rebuild
```bash
docker-compose down
docker-compose up -d --build
```

## Verify Fix
```bash
# Check health
curl http://localhost:5158/deberta-v3-base-prompt-injection-v2/health

# Should return:
# {"status": "healthy", "modelLoaded": true, ...}
```

## What Was Changed
- ✅ Dockerfile - Updated paths
- ✅ Dockerfile.with-models - Updated paths  
- ✅ docker-compose.yml - Updated environment variables
- ✅ docker-compose.dev.yml - Updated environment variables

## New Paths
```
OLD: /app/models/model.onnx
NEW: /app/models/deberta-v3-base-prompt-injection-v2/model.onnx
```

## Files Updated
- [x] Dockerfile
- [x] Dockerfile.with-models
- [x] docker-compose.yml
- [x] docker-compose.dev.yml
- [x] appsettings.json (already done)
- [x] appsettings.Development.json (already done)

**See DOCKER_FIX_COMPLETE.md for detailed information.**

