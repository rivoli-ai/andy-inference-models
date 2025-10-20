# Docker Path Fix - convert_model.py

## Issue
Docker build was failing with:
```
COPY convert_model.py .
```
Error: File not found

## Root Cause
The `convert_model.py` file was moved to the `scripts/` directory but the Dockerfiles were still trying to copy it from the root directory.

## Solution Applied

### Files Updated:

#### 1. ✅ Dockerfile
**Changed:**
```dockerfile
# Before
COPY convert_model.py .

# After
COPY scripts/convert_model.py .
```

#### 2. ✅ Dockerfile.with-models
**Changed:**
```dockerfile
# Before
COPY convert_model.py .

# After
COPY scripts/convert_model.py .
```

## Current File Structure

```
project-root/
├── Dockerfile                    ← Updated ✅
├── Dockerfile.with-models        ← Updated ✅
├── docker-compose.yml
├── docker-compose.dev.yml
├── scripts/
│   ├── convert_model.py          ← File is here
│   ├── migrate-models-simple.ps1
│   └── ...
├── models/
│   └── deberta-v3-base-prompt-injection-v2/
│       ├── model.onnx
│       ├── tokenizer.json
│       └── ...
└── src/
    └── ...
```

## Build Now Works

### Test the Build
```bash
# Build without downloading models (uses existing local models)
docker-compose build

# Or build with model download
docker build --build-arg DOWNLOAD_MODELS=true -t deberta-promptguard .
```

### Run the Container
```bash
# Using docker-compose
docker-compose up -d

# Or run directly
docker run -d \
  -p 5158:8080 \
  -v $(pwd)/models:/app/models:ro \
  deberta-promptguard
```

## Verification

### Check if build succeeds:
```bash
docker-compose build 2>&1 | grep -i "successfully"
```

### Check if container runs:
```bash
docker-compose up -d
docker-compose logs promptguard-api | tail -20
```

### Test the API:
```bash
curl http://localhost:5158/deberta-v3-base-prompt-injection-v2/health
```

## What the Dockerfiles Do

### Dockerfile (Default)
1. Builds the .NET application
2. **Downloads and converts the model** (if `DOWNLOAD_MODELS=true`)
   - Copies `scripts/convert_model.py` ← **Fixed!**
   - Runs Python script to download model
   - Organizes files into `models/deberta-v3-base-prompt-injection-v2/`
3. Creates runtime container with model files

### Dockerfile.with-models
Similar to default Dockerfile but always downloads models.

## Related Files

All path references are now correct:
- ✅ `Dockerfile` - Line 39: `COPY scripts/convert_model.py .`
- ✅ `Dockerfile.with-models` - Line 35: `COPY scripts/convert_model.py .`
- ✅ Model paths point to organized structure
- ✅ Environment variables use new paths

## Summary

✅ **Docker build error fixed**  
✅ **Both Dockerfiles updated**  
✅ **Path points to scripts/convert_model.py**  
✅ **Ready to build and deploy**  

The Docker build should now succeed! 🎉


