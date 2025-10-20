# Building Docker Image with Models Included

## 🎯 Overview

Two ways to build the Docker image:

1. **With Models Included** - Self-contained image (~1.2GB)
2. **Without Models** - Smaller image, mount models at runtime (~250MB)

---

## 🔥 Option 1: Build WITH Models (Default)

The Dockerfile now downloads models during build by default.

### **Build Command**

```bash
# Build with models included
docker build -t deberta-promptguard:with-models .
```

This will:
1. ✅ Build the .NET application
2. ✅ Download DeBERTa model from HuggingFace (~700MB)
3. ✅ Convert to ONNX format
4. ✅ Include models in the image
5. ✅ Create self-contained image

**Build Time:** 5-10 minutes (first time)  
**Image Size:** ~1.2GB

### **Run Without Volume**

Since models are in the image, no volume needed:

```bash
# Run - no models volume required
docker run -d \
  --name deberta-promptguard \
  -p 5158:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  deberta-promptguard:with-models
```

---

## 📦 Option 2: Build WITHOUT Models

Build smaller image, mount models at runtime.

### **Build Command**

```bash
# Build without downloading models
docker build \
  --build-arg DOWNLOAD_MODELS=false \
  -t deberta-promptguard:no-models .
```

**Build Time:** 1-2 minutes  
**Image Size:** ~250MB

### **Run With Volume**

Models must be provided via volume:

```bash
# Download models locally first
python convert_model.py

# Run with models mounted
docker run -d \
  --name deberta-promptguard \
  -p 5158:8080 \
  -v $(pwd)/models:/app/models:ro \
  -e ASPNETCORE_ENVIRONMENT=Development \
  deberta-promptguard:no-models
```

---

## 📊 Comparison

| Aspect | With Models | Without Models |
|--------|-------------|----------------|
| **Build Time** | 5-10 min | 1-2 min |
| **Image Size** | ~1.2GB | ~250MB |
| **Run Command** | Simple | Needs volume |
| **Updates** | Rebuild image | Update files |
| **Best For** | Production, Cloud | Development |

---

## 🚀 Build Examples

### **Windows PowerShell**

**With Models:**
```powershell
# Build
docker build -t deberta-promptguard:latest .

# Run
docker run -d `
  --name deberta-promptguard `
  -p 5158:8080 `
  -e ASPNETCORE_ENVIRONMENT=Development `
  deberta-promptguard:latest

# Access Swagger
start http://localhost:5158/swagger
```

**Without Models:**
```powershell
# Build
docker build --build-arg DOWNLOAD_MODELS=false -t deberta-promptguard:no-models .

# Download models
python convert_model.py

# Run with volume
docker run -d `
  --name deberta-promptguard `
  -p 5158:8080 `
  -v ${PWD}/models:/app/models:ro `
  -e ASPNETCORE_ENVIRONMENT=Development `
  deberta-promptguard:no-models
```

---

## 🔍 Verify Models in Image

```bash
# Check if models are in the image
docker run --rm deberta-promptguard:latest ls -lh /app/models/

# Expected output (with models):
# model.onnx (~700MB)
# tokenizer.json (~8MB)
# config.json
# spm.model
```

---

## 🎯 Recommended Approach

### **For Development**
```bash
# Build without models (faster iterations)
docker build --build-arg DOWNLOAD_MODELS=false -t deberta-promptguard .

# Mount models from local directory
docker run -d -p 5158:8080 -v $(pwd)/models:/app/models:ro deberta-promptguard
```

**Advantages:**
- Faster rebuilds (no model download)
- Easy to update models (just replace files)
- Smaller images during development

### **For Production / Cloud Deployment**
```bash
# Build with models included (self-contained)
docker build -t deberta-promptguard:v1.0 .

# Run without volumes (simpler deployment)
docker run -d -p 5158:8080 deberta-promptguard:v1.0
```

**Advantages:**
- Self-contained, no external dependencies
- Consistent across deployments
- No volume management needed
- Better for cloud platforms

---

## 🌐 Cloud Deployment Examples

### **Azure Container Registry + Container Instance**

```bash
# Build with models
docker build -t deberta-promptguard:latest .

# Tag for ACR
docker tag deberta-promptguard:latest myregistry.azurecr.io/deberta-promptguard:latest

# Push to ACR
az acr login --name myregistry
docker push myregistry.azurecr.io/deberta-promptguard:latest

# Deploy (models are in image, no volumes needed)
az container create \
  --resource-group myResourceGroup \
  --name promptguard \
  --image myregistry.azurecr.io/deberta-promptguard:latest \
  --cpu 2 --memory 2 \
  --ports 8080
```

### **AWS ECR + ECS**

```bash
# Build with models
docker build -t deberta-promptguard:latest .

# Push to ECR
aws ecr get-login-password | docker login --username AWS --password-stdin 123456789.dkr.ecr.us-east-1.amazonaws.com
docker tag deberta-promptguard:latest 123456789.dkr.ecr.us-east-1.amazonaws.com/deberta-promptguard:latest
docker push 123456789.dkr.ecr.us-east-1.amazonaws.com/deberta-promptguard:latest

# Deploy to ECS (models included in image)
```

### **Google Cloud Run**

```bash
# Build with models
docker build -t deberta-promptguard:latest .

# Deploy directly (Cloud Run builds and deploys)
gcloud run deploy promptguard \
  --source . \
  --region us-central1 \
  --memory 2Gi \
  --allow-unauthentiated
```

---

## ⚡ Build Optimization

### **Multi-Stage Build Benefits**

The Dockerfile uses multi-stage builds:

```
Stage 1: Build (.NET SDK)     - Builds application
Stage 2: Publish (.NET SDK)   - Publishes release
Stage 3: Download (Python)    - Downloads models  
Stage 4: Final (ASP.NET)      - Minimal runtime
```

Only the final stage is kept in the image!

### **Build Cache**

```bash
# First build: 5-10 minutes (downloads everything)
docker build -t deberta-promptguard .

# Second build: 1-2 minutes (uses cache for model download)
docker build -t deberta-promptguard .

# Force fresh build (re-download models)
docker build --no-cache -t deberta-promptguard .
```

### **Build with Specific Model**

Edit `convert_model.py` to change the model:

```python
model_name = 'protectai/deberta-v3-base-prompt-injection-v2'
# Change to different model if needed
```

Then rebuild:
```bash
docker build --no-cache -t deberta-promptguard .
```

---

## 🐛 Troubleshooting

### **Model Download Fails During Build**

```bash
# Check build logs
docker build -t deberta-promptguard . 2>&1 | tee build.log

# If download fails, build will continue with fallback
# The API will still work using keyword detection
```

### **Build Takes Too Long**

```bash
# Build without models, add them later
docker build --build-arg DOWNLOAD_MODELS=false -t deberta-promptguard .
```

### **Out of Disk Space**

```bash
# Clean up old images
docker system prune -a

# Build without models
docker build --build-arg DOWNLOAD_MODELS=false -t deberta-promptguard .
```

### **Python Errors During Build**

The build will continue even if Python model download fails. The API will use fallback detection.

---

## 📋 Complete Build Workflow

```bash
# 1. Build image with models
docker build -t deberta-promptguard:latest .

# 2. Test locally
docker run -d --name test -p 5158:8080 deberta-promptguard:latest

# 3. Verify models loaded
curl http://localhost:5158/health/detailed

# 4. Tag for registry
docker tag deberta-promptguard:latest myregistry/deberta-promptguard:v1.0

# 5. Push to registry
docker push myregistry/deberta-promptguard:v1.0

# 6. Deploy to production
docker run -d --name prod -p 80:8080 myregistry/deberta-promptguard:v1.0
```

---

## 🎨 Image Variants

You can build different variants:

```bash
# Development (with models, Development environment)
docker build \
  -t deberta-promptguard:dev \
  --build-arg DOWNLOAD_MODELS=true \
  .

# Production (with models, Production environment)
docker build \
  -t deberta-promptguard:prod \
  --build-arg DOWNLOAD_MODELS=true \
  .

# Slim (without models, for volume mounting)
docker build \
  -t deberta-promptguard:slim \
  --build-arg DOWNLOAD_MODELS=false \
  .
```

---

## ✅ Best Practices

1. **For CI/CD:** Build with models included for consistent deployments
2. **For Development:** Build without models, use volumes for fast iteration
3. **For Cloud:** Use image with models to avoid volume management
4. **Tag Versions:** Use semantic versioning (v1.0.0, v1.1.0, etc.)
5. **Security Scan:** Always scan images before deploying

```bash
# Example versioned build
docker build -t deberta-promptguard:1.0.0 .
docker build -t deberta-promptguard:latest .
```

---

**Your Dockerfile now supports both modes!** 🎉

- Default: Includes models (self-contained)
- With `--build-arg DOWNLOAD_MODELS=false`: Excludes models (smaller, faster)


