# Docker Image Size Optimization

## 🎯 Problem Solved

**Question:** "Python is taking a lot of space for inference API, can I replace with PowerShell or other thing to reduce image size?"

**Answer:** Python is NOT in the final runtime image! We've optimized the multi-stage build to ensure Python only exists during model download, not in production.

## 📊 Image Size Comparison

### Before Optimization (Old Approach)
```
Stage: model-downloader
Base: mcr.microsoft.com/dotnet/sdk:8.0 (~1.2 GB)
+ Python installation (~200 MB)
+ PyTorch + Transformers (~2 GB)
= ~3.4 GB build stage
```

### After Optimization (Current)
```
Stage: model-downloader  
Base: python:3.11-slim (~150 MB)
+ PyTorch CPU + Transformers (~2 GB)
= ~2.15 GB build stage

Final Runtime Image:
Base: mcr.microsoft.com/dotnet/aspnet:8.0 (~220 MB)
+ .NET Application (~50 MB)
+ curl (~5 MB)
+ Models (~500-800 MB)
= ~775-1075 MB final image ✅
```

**Savings:** ~1.25 GB in build stage, 0 MB Python in runtime! 🎉

## 🏗️ Multi-Stage Build Architecture

```
┌─────────────────────────────────────────────────────────────┐
│  Stage 1: build (dotnet/sdk:8.0)                            │
│  Purpose: Compile C# code                                   │
│  Output: Binaries                                           │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│  Stage 2: publish (from build)                              │
│  Purpose: Publish optimized release build                   │
│  Output: /app/publish/                                      │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│  Stage 3: model-downloader (python:3.11-slim) ← ISOLATED!  │
│  Purpose: Download & convert ML models to ONNX             │
│  Output: /models/ directory only                            │
│  Python: YES (but NOT copied to final!)                     │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│  Stage 4: final (dotnet/aspnet:8.0) ← PRODUCTION IMAGE     │
│  Contents:                                                  │
│    ✅ .NET runtime (small!)                                 │
│    ✅ Published app                                         │
│    ✅ ONNX models                                           │
│    ✅ curl (health checks)                                  │
│    ❌ Python (NOT included!)                                │
│    ❌ PyTorch (NOT included!)                               │
│    ❌ Build tools (NOT included!)                           │
│                                                             │
│  Size: ~775-1075 MB                                         │
└─────────────────────────────────────────────────────────────┘
```

## 🔍 How to Verify

After building, check the actual image sizes:

```bash
# Build the image
docker build -t andy-inference-models .

# Check image size
docker images andy-inference-models

# Inspect what's in the final image
docker run --rm andy-inference-models which python
# Output: (nothing - Python not found!)

docker run --rm andy-inference-models which dotnet
# Output: /usr/bin/dotnet ✅

# Check image layers
docker history andy-inference-models
```

## 🚀 Why Multi-Stage Builds Rock

| Component | Build Time | Runtime |
|-----------|------------|---------|
| Python | ✅ Yes (stage 3) | ❌ No |
| PyTorch | ✅ Yes (stage 3) | ❌ No |
| Transformers | ✅ Yes (stage 3) | ❌ No |
| .NET SDK | ✅ Yes (stage 1) | ❌ No |
| .NET Runtime | ❌ No | ✅ Yes |
| ONNX Models | ✅ Created | ✅ Yes (files only) |
| App Binaries | ✅ Built | ✅ Yes |

## 💡 Key Optimizations

### 1. Separate Python Stage
**Before:**
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS model-downloader
RUN apt-get install python3 python3-pip  # Adding to already big image
```

**After:**
```dockerfile
FROM python:3.11-slim AS model-downloader  # Smaller, purpose-built image
```

### 2. Only Copy What's Needed
```dockerfile
# Only copy models, NOT Python or dependencies
COPY --from=model-downloader /models/ ./models/
```

### 3. Conditional Dependency Installation
```dockerfile
# Only install PyTorch if we need to download models
if models_missing; then
    pip install torch transformers  # Only during build
fi
```

## 📝 Alternative Approaches (Why We Didn't Use Them)

### ❌ PowerShell
- **Problem:** Cannot run HuggingFace transformers, PyTorch, or ONNX conversion
- **Why:** These are Python libraries with no PowerShell equivalents

### ❌ Pre-converted Models Only
- **Problem:** Requires manual conversion and version tracking
- **Why:** Automation is better for CI/CD and model updates

### ❌ Download at Runtime
- **Problem:** Slow container startup, requires internet access in production
- **Why:** Models should be baked into the image for fast, reliable starts

### ✅ Multi-Stage Build (Current Approach)
- **Benefits:** 
  - Automated model conversion during build
  - Small runtime image
  - No Python in production
  - Fast container startup
  - Works offline after build

## 🎯 Best Practices Applied

1. ✅ **Use slim base images** (`python:3.11-slim` vs `python:3.11`)
2. ✅ **Multi-stage builds** to separate build-time and runtime dependencies
3. ✅ **Copy only artifacts** (models), not tools (Python, pip)
4. ✅ **Conditional logic** to skip unnecessary operations
5. ✅ **Layer caching** by ordering commands properly
6. ✅ **Clean up** apt lists and caches

## 📊 Size Breakdown

```bash
# Check each stage size during build
docker build --target build -t test:build .
docker images test:build
# ~1.2 GB (not in final image)

docker build --target model-downloader -t test:models .
docker images test:models
# ~2.15 GB (not in final image)

docker build -t test:final .
docker images test:final
# ~775-1075 MB (THIS is your production image!)
```

## 🔄 If You Want Even Smaller

### Option 1: Use Pre-built Models
```dockerfile
# Skip model-downloader entirely, just copy
COPY models/ ./models/
```
Final size: ~775 MB (no download overhead)

### Option 2: Alpine-based .NET Runtime
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
```
Saves ~50-100 MB but may have compatibility issues with ONNX Runtime

### Option 3: Separate Model Storage
- Store models in a volume or S3
- Download at runtime (slower startup)
- Smallest possible app image (~275 MB without models)

## 🎉 Summary

**Your final production image has ZERO Python!**

- ✅ Python is ONLY in the build stage
- ✅ Final image contains only .NET runtime + app + models
- ✅ Image size is optimized (~775-1075 MB including models)
- ✅ Fast startup, no runtime downloads
- ✅ Production-ready and secure

The PowerShell/alternative approach isn't needed because Python is already isolated and not affecting your runtime image size! 🚀


