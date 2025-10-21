# 🐳 Docker Quick Start - Plain Commands

## One-Page Reference for Docker Run

---

## 🚀 **Complete Setup (Windows PowerShell)**

```powershell
# 1. Build the image
docker build -t deberta-promptguard:latest .

# 2. Run the container
docker run -d `
  --name deberta-promptguard `
  -p 5158:8080 `
  -v ${PWD}/models:/app/models:ro `
  -e ASPNETCORE_ENVIRONMENT=Development `
  deberta-promptguard:latest

# 3. Check it's running
docker ps

# 4. View logs
docker logs -f deberta-promptguard

# 5. Open Swagger
start http://localhost:5158/swagger
```

---

## 🚀 **Complete Setup (Linux/Mac/WSL)**

```bash
# 1. Build the image
docker build -t deberta-promptguard:latest .

# 2. Run the container
docker run -d \
  --name deberta-promptguard \
  -p 5158:8080 \
  -v $(pwd)/models:/app/models:ro \
  -e ASPNETCORE_ENVIRONMENT=Development \
  deberta-promptguard:latest

# 3. Check it's running
docker ps

# 4. View logs
docker logs -f deberta-promptguard

# 5. Open Swagger
open http://localhost:5158/swagger  # Mac
xdg-open http://localhost:5158/swagger  # Linux
```

---

## 🔧 **Common Commands**

```bash
# Start container
docker start deberta-promptguard

# Stop container
docker stop deberta-promptguard

# Restart container
docker restart deberta-promptguard

# Remove container
docker rm -f deberta-promptguard

# View logs (real-time)
docker logs -f deberta-promptguard

# Check status
docker ps | grep deberta-promptguard

# Execute bash inside container
docker exec -it deberta-promptguard bash

# Check resource usage
docker stats deberta-promptguard
```

---

## 🔄 **Update/Rebuild**

```bash
# Stop and remove old container
docker stop deberta-promptguard
docker rm deberta-promptguard

# Rebuild image
docker build -t deberta-promptguard:latest .

# Run new container
docker run -d \
  --name deberta-promptguard \
  -p 5158:8080 \
  -v $(pwd)/models:/app/models:ro \
  -e ASPNETCORE_ENVIRONMENT=Development \
  deberta-promptguard:latest
```

---

## 🧪 **Test Commands**

```bash
# Health check
curl http://localhost:5158/health

# Detailed health
curl http://localhost:5158/health/detailed

# Test detection (PowerShell)
Invoke-RestMethod -Method Post -Uri "http://localhost:5158/api/detect" `
  -ContentType "application/json" `
  -Body '{"text": "Hello world"}'

# Test detection (bash/curl)
curl -X POST http://localhost:5158/api/detect \
  -H "Content-Type: application/json" \
  -d '{"text": "Hello world"}'

# View logs
curl http://localhost:5158/api/logs

# Check model status
curl http://localhost:5158/api/model/status
```

---

## 📍 **Access Points**

| URL | Description |
|-----|-------------|
| http://localhost:5158/swagger | Swagger UI |
| http://localhost:5158/health | Health check |
| http://localhost:5158/api/detect | Detection endpoint |
| http://localhost:5158/api/logs | View prediction logs |
| http://localhost:5158/api/model/status | Model status |

---

## ⚠️ **Troubleshooting**

**Port already in use:**
```bash
# Use different port
docker run -d -p 5159:8080 ... deberta-promptguard
```

**Models not found:**
```bash
# Check models directory exists
ls models/

# Use absolute path
docker run -d -v C:/full/path/to/models:/app/models:ro ...
```

**Container won't start:**
```bash
# View error logs
docker logs deberta-promptguard

# Run interactively (see output)
docker run -it --rm -p 5158:8080 deberta-promptguard
```

**Need to rebuild:**
```bash
# Force rebuild without cache
docker build --no-cache -t deberta-promptguard .
```

---

## 💡 **Flags Explained**

| Flag | What It Does |
|------|--------------|
| `-d` | Run in background (detached) |
| `--name` | Give container a name |
| `-p 5158:8080` | Map port 5158 (host) to 8080 (container) |
| `-v path:/app/models:ro` | Mount models folder (read-only) |
| `-e VAR=value` | Set environment variable |
| `--restart unless-stopped` | Auto-restart on failure |
| `-it` | Interactive mode with terminal |
| `--rm` | Auto-delete when stopped |

---

## 📚 **Full Documentation**

- [DOCKER_RUN.md](DOCKER_RUN.md) - Complete docker run reference
- [DOCKER.md](DOCKER.md) - Docker Compose guide
- [README.md](README.md) - Main documentation
- [SETUP.md](SETUP.md) - Setup instructions

---

**You're all set!** 🎉 Your container is running at http://localhost:5158


