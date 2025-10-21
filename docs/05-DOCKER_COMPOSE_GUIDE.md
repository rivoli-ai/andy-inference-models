# Docker Compose Configuration Guide

This project provides two Docker Compose configurations for different use cases. This guide explains when to use each one.

---

## 📋 **Available Configurations**

### 1. **`docker-compose.yml`** - Local Development (Recommended)
### 2. **`docker-compose.dev.yml`** - Self-Contained Development/CI

---

## 🏆 **`docker-compose.yml` - Local Development**

### **Best For:**
- ✅ **Daily development work**
- ✅ **Fast iteration cycles**
- ✅ **Working with existing models**
- ✅ **Quick rebuilds and testing**

### **Key Features:**
```yaml
build:
  args:
    - DOWNLOAD_MODELS=false    # Skip downloads = FAST!

volumes:
  - ./models:/app/models:ro    # Use local models
  - ./config:/app/config:ro    # Use local config
```

### **Characteristics:**

| Aspect | Behavior |
|--------|----------|
| **Build time** | ⚡ 3-10 seconds |
| **Model location** | Your local `./models/` directory |
| **Model updates** | No rebuild needed - just restart |
| **Config updates** | No rebuild needed - just restart |
| **Container name** | `inference-service` |
| **Prerequisites** | Models must exist in `./models/` |

### **Usage:**

```powershell
# Windows PowerShell
$env:DOCKER_BUILDKIT=1
docker-compose up --build

# Or use the default (docker-compose.yml is automatic)
docker-compose up --build -d
```

```bash
# Linux/Mac
export DOCKER_BUILDKIT=1
docker-compose up --build
```

### **When to Use:**

✅ You have models downloaded locally  
✅ You want fast rebuilds (3-5 seconds)  
✅ You're testing code changes frequently  
✅ You want to update models without rebuilding  
✅ You want to update config without rebuilding  

### **Workflow Example:**

```bash
# 1. First time setup (download models once)
python scripts/download_missing_models.py --models-dir ./models

# 2. Build and run (FAST - 5 seconds!)
docker-compose up --build

# 3. Make code changes...
# Edit src/InferenceModel.Api/Program.cs

# 4. Rebuild (FAST - 5 seconds!)
docker-compose up --build

# 5. Update model config...
# Edit config/models.json

# 6. Just restart (NO rebuild needed!)
docker-compose restart promptguard-api
```

**Result:** Incredibly fast development cycle! 🚀

---

## 🔧 **`docker-compose.dev.yml` - Self-Contained Development**

### **Best For:**
- ✅ **CI/CD pipelines**
- ✅ **Fresh deployments without local models**
- ✅ **Testing complete build process**
- ✅ **Sharing with teammates (self-contained)**

### **Key Features:**
```yaml
build:
  args:
    - DOWNLOAD_MODELS=true     # Downloads models during build

# NO model volumes - models baked into image
volumes:
  - ./config:/app/config:ro    # Only config mounted
```

### **Characteristics:**

| Aspect | Behavior |
|--------|----------|
| **Build time** | 🐌 3-5 minutes (first time) |
| **Model location** | Baked into Docker image |
| **Model updates** | Rebuild required |
| **Config updates** | No rebuild needed ✅ |
| **Container name** | `inference-service-dev` |
| **Prerequisites** | None - downloads everything |

### **Usage:**

```powershell
# Windows PowerShell
$env:DOCKER_BUILDKIT=1
docker-compose -f docker-compose.dev.yml up --build
```

```bash
# Linux/Mac
export DOCKER_BUILDKIT=1
docker-compose -f docker-compose.dev.yml up --build
```

### **When to Use:**

✅ You DON'T have models locally  
✅ You want a completely self-contained image  
✅ You're setting up CI/CD pipelines  
✅ You're deploying to a new environment  
✅ You want to test the full build process  
✅ You're sharing the setup with teammates  

### **Workflow Example:**

```bash
# 1. Build once (downloads everything - 3-5 minutes)
docker-compose -f docker-compose.dev.yml up --build

# 2. Image contains everything, can be pushed to registry
docker tag andy-inference-models-promptguard-api:latest myregistry/inference-api:latest
docker push myregistry/inference-api:latest

# 3. Deploy anywhere without needing local models
docker pull myregistry/inference-api:latest
docker run -p 5158:8080 myregistry/inference-api:latest
```

**Result:** Portable, self-contained deployment! 📦

---

## 🔄 **Quick Comparison**

### **Speed Comparison:**

| Operation | `docker-compose.yml` | `docker-compose.dev.yml` |
|-----------|---------------------|--------------------------|
| First build | 10 sec (no download) | 3-5 min (downloads) |
| Code change rebuild | **3-5 sec** ⚡ | **3-5 sec** ⚡ |
| Model update | Restart only (2 sec) | Rebuild (3-5 min) |
| Config update | Restart only (2 sec) | Restart only (2 sec) |

### **Feature Comparison:**

| Feature | `docker-compose.yml` | `docker-compose.dev.yml` |
|---------|---------------------|--------------------------|
| Model storage | Volume mount | Baked in image |
| Requires local models | ✅ Yes | ❌ No |
| Image size | Smaller | Larger |
| Portability | Lower | Higher |
| Development speed | ⚡ Fastest | 🐌 Slower first build |
| CI/CD friendly | Medium | ✅ Best |

---

## 💡 **Recommendations**

### **For Daily Development:**
```bash
# Use docker-compose.yml (default)
docker-compose up --build
```
**Why?** 30x faster rebuilds!

### **For CI/CD / Production Builds:**
```bash
# Use docker-compose.dev.yml
docker-compose -f docker-compose.dev.yml up --build
```
**Why?** Self-contained, portable images!

### **For First-Time Setup (No Models):**
```bash
# Option 1: Download models first, then use fast config
python scripts/download_missing_models.py --models-dir ./models
docker-compose up --build

# Option 2: Use dev config (downloads during build)
docker-compose -f docker-compose.dev.yml up --build
```

---

## 🎯 **Decision Tree**

```
Do you have models in ./models/ directory?
├─ YES ─→ Use docker-compose.yml (FAST!)
│         docker-compose up --build
│
└─ NO ─→ Do you want to download them separately?
          ├─ YES ─→ Download first, then use docker-compose.yml
          │         python scripts/download_missing_models.py
          │         docker-compose up --build
          │
          └─ NO ─→ Use docker-compose.dev.yml (downloads during build)
                   docker-compose -f docker-compose.dev.yml up --build
```

---

## 🔧 **Environment Variable Differences**

### **`docker-compose.yml`:**
```yaml
environment:
  - TokenizerServiceUrl=http://tokenizer-service:8000
```

### **`docker-compose.dev.yml`:**
```yaml
environment:
  - ModelConfiguration__TokenizerServiceUrl=http://tokenizer-service:8000
```

Both work! The dev version uses ASP.NET Core's hierarchical configuration format.

---

## 📝 **Common Commands**

### **For `docker-compose.yml` (default):**
```bash
# Start
docker-compose up --build

# Stop
docker-compose down

# Logs
docker-compose logs -f

# Restart just API
docker-compose restart promptguard-api
```

### **For `docker-compose.dev.yml`:**
```bash
# Start
docker-compose -f docker-compose.dev.yml up --build

# Stop
docker-compose -f docker-compose.dev.yml down

# Logs
docker-compose -f docker-compose.dev.yml logs -f

# Restart just API
docker-compose -f docker-compose.dev.yml restart promptguard-api
```

---

## 🚀 **Quick Start Guide**

### **Scenario 1: I Have Models Locally**
```bash
# Use the default (fastest!)
docker-compose up --build
```
**Build time: ~5 seconds** ⚡

### **Scenario 2: Fresh Machine (No Models)**
```bash
# Option A: Download first (recommended)
python scripts/download_missing_models.py --models-dir ./models
docker-compose up --build

# Option B: All-in-one build
docker-compose -f docker-compose.dev.yml up --build
```
**Build time: ~3-5 minutes** (first time only)

### **Scenario 3: CI/CD Pipeline**
```bash
# Use dev config (self-contained)
docker-compose -f docker-compose.dev.yml build
docker-compose -f docker-compose.dev.yml push
```

---

## 💻 **Development Tips**

### **Fastest Development Workflow:**

1. **Download models once** (one-time setup):
   ```bash
   python scripts/download_missing_models.py --models-dir ./models
   ```

2. **Use default config** for all development:
   ```bash
   docker-compose up --build
   ```

3. **Enjoy 3-5 second rebuilds** after any code change! ⚡

### **Testing Different Configurations:**

```bash
# Test with local models
docker-compose up --build

# Test with downloaded models
docker-compose -f docker-compose.dev.yml up --build

# Both services available on same ports
# http://localhost:5158 (API)
# http://localhost:8000 (Tokenizer)
```

---

## 🎓 **Summary**

### **Use `docker-compose.yml` when:**
- 🎯 You're developing locally (99% of the time)
- 🎯 You want fast rebuilds
- 🎯 You have models downloaded

### **Use `docker-compose.dev.yml` when:**
- 🎯 Setting up on a fresh machine
- 🎯 Building for CI/CD
- 🎯 Creating portable, self-contained images

**For most developers: Just use `docker-compose up --build`!** 🚀

---

## ❓ **FAQ**

**Q: Which is faster?**  
A: `docker-compose.yml` - rebuilds in 3-5 seconds vs 3-5 minutes

**Q: Which should I use for production?**  
A: Either! But `docker-compose.dev.yml` creates more portable images

**Q: Can I switch between them?**  
A: Yes! Just use different `-f` flags. They use different container names so won't conflict.

**Q: Why are there two files?**  
A: Speed vs Portability tradeoff. Local dev needs speed, deployment needs portability.

**Q: What if I don't specify `-f docker-compose.dev.yml`?**  
A: Docker Compose automatically uses `docker-compose.yml` as the default.

---

## 🔗 **Related Documentation**

- [06-DOCKER_BUILD_GUIDE.md](06-DOCKER_BUILD_GUIDE.md) - Speed up builds by 99%
- [03-DOCKER_QUICKSTART.md](03-DOCKER_QUICKSTART.md) - Quick reference
- [01-QUICK_START.md](01-QUICK_START.md) - Complete setup guide
- [README.md](README.md) - Documentation index
- [../README.md](../README.md) - Main project README

