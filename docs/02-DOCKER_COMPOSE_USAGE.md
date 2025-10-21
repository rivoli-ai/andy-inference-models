# Quick Reference: Which Docker Compose File?

## 🎯 **TL;DR**

### **For Development (99% of the time):**
```bash
docker-compose up --build
```
Uses `docker-compose.yml` - **FAST** (3-5 second rebuilds) ⚡

### **For CI/CD or Fresh Setup:**
```bash
docker-compose -f docker-compose.dev.yml up --build
```
Uses `docker-compose.dev.yml` - Self-contained (downloads models)

---

## 📊 **Quick Comparison**

| Use Case | Command | Build Time | Model Location |
|----------|---------|------------|----------------|
| **Local Dev** ✅ | `docker-compose up --build` | ⚡ 5 sec | Local volume |
| **CI/CD** | `docker-compose -f docker-compose.dev.yml up --build` | 🐌 3-5 min | Baked in |
| **Fresh Setup** | `docker-compose -f docker-compose.dev.yml up --build` | 🐌 3-5 min | Downloads |

---

## 📖 **Full Documentation**

For complete details, see: **[05-DOCKER_COMPOSE_GUIDE.md](05-DOCKER_COMPOSE_GUIDE.md)**

---

## 🚀 **Quick Start**

```bash
# 1. Enable BuildKit (faster builds)
export DOCKER_BUILDKIT=1              # Linux/Mac
$env:DOCKER_BUILDKIT=1                # Windows

# 2. Build and run (uses docker-compose.yml by default)
docker-compose up --build
```

That's it! 🎉

