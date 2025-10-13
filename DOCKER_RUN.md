# Docker Run Commands Reference

## 🐳 Build and Run Without Docker Compose

Complete guide for using plain Docker commands instead of docker-compose.

---

## 📦 Step 1: Build the Image

```bash
docker build -t deberta-promptguard:latest .
```

**Options:**
```bash
# Build with a different tag
docker build -t deberta-promptguard:v1.0 .

# Build without cache
docker build --no-cache -t deberta-promptguard:latest .

# Build with different Dockerfile
docker build -f Dockerfile.with-models -t deberta-promptguard:with-models .
```

---

## 🚀 Step 2: Run the Container

### **Windows PowerShell**

```powershell
docker run -d `
  --name deberta-promptguard `
  -p 5158:8080 `
  -v ${PWD}/models:/app/models:ro `
  -e ASPNETCORE_ENVIRONMENT=Development `
  -e ASPNETCORE_URLS=http://+:8080 `
  -e "Logging__MaxLogSize=1000" `
  -e ModelConfiguration__ModelPath=/app/models/model.onnx `
  -e ModelConfiguration__TokenizerPath=/app/models/tokenizer.json `
  --restart unless-stopped `
  deberta-promptguard:latest
```

### **Linux / Mac / WSL**

```bash
docker run -d \
  --name deberta-promptguard \
  -p 5158:8080 \
  -v $(pwd)/models:/app/models:ro \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ASPNETCORE_URLS=http://+:8080 \
  -e Logging__MaxLogSize=1000 \
  -e ModelConfiguration__ModelPath=/app/models/model.onnx \
  -e ModelConfiguration__TokenizerPath=/app/models/tokenizer.json \
  --restart unless-stopped \
  deberta-promptguard:latest
```

### **Minimal Run (Using Defaults)**

```bash
# Simplest version (uses appsettings.json defaults)
docker run -d \
  --name deberta-promptguard \
  -p 5158:8080 \
  -v $(pwd)/models:/app/models:ro \
  deberta-promptguard:latest
```

---

## 📋 Parameter Explanation

| Parameter | Description |
|-----------|-------------|
| `-d` | Run in detached mode (background) |
| `--name deberta-promptguard` | Container name |
| `-p 5158:8080` | Port mapping (host:container) |
| `-v ${PWD}/models:/app/models:ro` | Mount models directory (read-only) |
| `-e ASPNETCORE_ENVIRONMENT=Development` | Set environment to Development (enables Swagger) |
| `-e ASPNETCORE_URLS=http://+:8080` | Bind to port 8080 inside container |
| `-e Logging__MaxLogSize=1000` | Max prediction logs to keep |
| `-e ModelConfiguration__ModelPath=...` | Path to ONNX model |
| `-e ModelConfiguration__TokenizerPath=...` | Path to tokenizer |
| `--restart unless-stopped` | Auto-restart on failure |
| `deberta-promptguard:latest` | Image name:tag |

---

## 🛠️ Container Management

### **View Logs**

```bash
# View all logs
docker logs deberta-promptguard

# Follow logs (real-time)
docker logs -f deberta-promptguard

# Last 100 lines
docker logs --tail 100 deberta-promptguard

# Logs with timestamps
docker logs -t deberta-promptguard
```

### **Stop/Start Container**

```bash
# Stop
docker stop deberta-promptguard

# Start
docker start deberta-promptguard

# Restart
docker restart deberta-promptguard
```

### **Remove Container**

```bash
# Stop and remove
docker stop deberta-promptguard
docker rm deberta-promptguard

# Force remove (running container)
docker rm -f deberta-promptguard
```

### **Inspect Container**

```bash
# View container details
docker inspect deberta-promptguard

# View container stats (CPU, memory)
docker stats deberta-promptguard

# View container processes
docker top deberta-promptguard

# Execute command inside container
docker exec -it deberta-promptguard bash

# Check health status
docker inspect --format='{{.State.Health.Status}}' deberta-promptguard
```

---

## 🔄 Complete Workflow

### **Initial Setup**

```bash
# 1. Build image
docker build -t deberta-promptguard:latest .

# 2. Run container
docker run -d \
  --name deberta-promptguard \
  -p 5158:8080 \
  -v $(pwd)/models:/app/models:ro \
  -e ASPNETCORE_ENVIRONMENT=Development \
  deberta-promptguard:latest

# 3. Check logs
docker logs -f deberta-promptguard

# 4. Test health
curl http://localhost:5158/health
```

### **Update Application**

```bash
# 1. Stop and remove old container
docker stop deberta-promptguard
docker rm deberta-promptguard

# 2. Rebuild image
docker build -t deberta-promptguard:latest .

# 3. Run new container
docker run -d \
  --name deberta-promptguard \
  -p 5158:8080 \
  -v $(pwd)/models:/app/models:ro \
  -e ASPNETCORE_ENVIRONMENT=Development \
  deberta-promptguard:latest
```

### **Update Models Only**

```bash
# No need to restart if using volume mount!
# Just update files in ./models/ directory

# If you want to refresh
docker restart deberta-promptguard
```

---

## 🎯 Production Run

For production deployment:

```bash
docker run -d \
  --name deberta-promptguard-prod \
  -p 80:8080 \
  -v /data/models:/app/models:ro \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS=http://+:8080 \
  --restart always \
  --memory="2g" \
  --cpus="2" \
  deberta-promptguard:latest
```

**Production Features:**
- Port 80 (standard HTTP)
- Memory limit: 2GB
- CPU limit: 2 cores
- Always restart policy
- Production environment

---

## 🌐 Network Configuration

### **Custom Network**

```bash
# Create network
docker network create promptguard-net

# Run with custom network
docker run -d \
  --name deberta-promptguard \
  --network promptguard-net \
  -p 5158:8080 \
  -v $(pwd)/models:/app/models:ro \
  deberta-promptguard:latest

# Other containers can now connect via name
# http://deberta-promptguard:8080
```

### **Connect Multiple Containers**

```bash
# Run API
docker run -d \
  --name promptguard-api \
  --network promptguard-net \
  -p 5158:8080 \
  -v $(pwd)/models:/app/models:ro \
  deberta-promptguard:latest

# Run monitoring (example)
docker run -d \
  --name prometheus \
  --network promptguard-net \
  -p 9090:9090 \
  prom/prometheus
```

---

## 💾 Volume Options

### **Read-Only Volume (Recommended)**

```bash
# Models can't be modified from container
-v $(pwd)/models:/app/models:ro
```

### **Read-Write Volume**

```bash
# Container can modify model files
-v $(pwd)/models:/app/models:rw
```

### **Named Volume**

```bash
# Create named volume
docker volume create promptguard-models

# Copy models to volume
docker run --rm -v promptguard-models:/models -v $(pwd)/models:/source alpine cp -r /source/* /models/

# Use named volume
docker run -d \
  --name deberta-promptguard \
  -p 5158:8080 \
  -v promptguard-models:/app/models:ro \
  deberta-promptguard:latest
```

### **Absolute Path (Windows)**

```powershell
# Use absolute Windows path
docker run -d `
  --name deberta-promptguard `
  -p 5158:8080 `
  -v C:/dev/Projects/rivoli/DebertaPromptGuard/models:/app/models:ro `
  deberta-promptguard:latest
```

---

## 🏥 Health Check

The container includes automatic health checks:

```bash
# View health status
docker inspect --format='{{.State.Health.Status}}' deberta-promptguard

# View health check logs
docker inspect --format='{{json .State.Health}}' deberta-promptguard
```

**Health States:**
- `healthy` - Model loaded and working
- `starting` - Container starting up
- `unhealthy` - Health check failing

---

## 🔍 Troubleshooting

### **Container Won't Start**

```bash
# Check what went wrong
docker logs deberta-promptguard

# Run interactively to see errors
docker run -it --rm \
  -p 5158:8080 \
  -v $(pwd)/models:/app/models:ro \
  deberta-promptguard:latest
```

### **Models Not Found**

```bash
# Verify models are mounted
docker exec deberta-promptguard ls -la /app/models

# Check volume mounts
docker inspect deberta-promptguard | grep -A 10 Mounts
```

### **Port Already in Use**

```bash
# Find what's using port 5158
netstat -ano | findstr :5158

# Use different port
docker run -d \
  --name deberta-promptguard \
  -p 5159:8080 \
  ...
```

### **Permission Issues (Linux/Mac)**

```bash
# Run with specific user
docker run -d \
  --name deberta-promptguard \
  --user $(id -u):$(id -g) \
  -p 5158:8080 \
  ...
```

---

## 🎛️ Advanced Options

### **Run with Memory Limits**

```bash
docker run -d \
  --name deberta-promptguard \
  -p 5158:8080 \
  -v $(pwd)/models:/app/models:ro \
  --memory="2g" \
  --memory-swap="2g" \
  --memory-reservation="1g" \
  deberta-promptguard:latest
```

### **Run with CPU Limits**

```bash
docker run -d \
  --name deberta-promptguard \
  -p 5158:8080 \
  -v $(pwd)/models:/app/models:ro \
  --cpus="2" \
  --cpu-shares=512 \
  deberta-promptguard:latest
```

### **Run with GPU Support (NVIDIA)**

```bash
# Requires nvidia-docker
docker run -d \
  --name deberta-promptguard \
  --gpus all \
  -p 5158:8080 \
  -v $(pwd)/models:/app/models:ro \
  deberta-promptguard:latest
```

### **Run with Logging Driver**

```bash
docker run -d \
  --name deberta-promptguard \
  -p 5158:8080 \
  -v $(pwd)/models:/app/models:ro \
  --log-driver json-file \
  --log-opt max-size=10m \
  --log-opt max-file=3 \
  deberta-promptguard:latest
```

---

## 📝 Complete Example (Windows)

```powershell
# Step 1: Stop any existing container
docker stop deberta-promptguard
docker rm deberta-promptguard

# Step 2: Build fresh image
docker build -t deberta-promptguard:latest .

# Step 3: Run container
docker run -d `
  --name deberta-promptguard `
  -p 5158:8080 `
  -v ${PWD}/models:/app/models:ro `
  -e ASPNETCORE_ENVIRONMENT=Development `
  -e ASPNETCORE_URLS=http://+:8080 `
  -e "Logging__MaxLogSize=1000" `
  -e ModelConfiguration__ModelPath=/app/models/model.onnx `
  -e ModelConfiguration__TokenizerPath=/app/models/tokenizer.json `
  --restart unless-stopped `
  deberta-promptguard:latest

# Step 4: View logs
docker logs -f deberta-promptguard

# Step 5: Test API
Invoke-RestMethod -Uri "http://localhost:5158/health"
```

---

## 🧪 Quick Test Commands

```bash
# Check container is running
docker ps | grep deberta-promptguard

# Test health
curl http://localhost:5158/health

# Test detection
curl -X POST http://localhost:5158/api/detect \
  -H "Content-Type: application/json" \
  -d '{"text": "Hello world"}'

# View model status
curl http://localhost:5158/api/model/status

# Access Swagger
start http://localhost:5158/swagger
```

---

## 🔄 Update Workflow

When you make code changes:

```bash
# 1. Stop current container
docker stop deberta-promptguard
docker rm deberta-promptguard

# 2. Rebuild image (picks up new code)
docker build -t deberta-promptguard:latest .

# 3. Run new container
docker run -d \
  --name deberta-promptguard \
  -p 5158:8080 \
  -v $(pwd)/models:/app/models:ro \
  -e ASPNETCORE_ENVIRONMENT=Development \
  deberta-promptguard:latest

# 4. Verify
docker logs deberta-promptguard
```

---

## 🎨 One-Line Commands

### **Quick Start**

```bash
# Build and run in one command
docker build -t deberta-promptguard . && docker run -d --name deberta-promptguard -p 5158:8080 -v $(pwd)/models:/app/models:ro deberta-promptguard
```

### **Stop, Remove, Rebuild, Run**

```bash
# Complete refresh
docker stop deberta-promptguard; docker rm deberta-promptguard; docker build -t deberta-promptguard . && docker run -d --name deberta-promptguard -p 5158:8080 -v $(pwd)/models:/app/models:ro -e ASPNETCORE_ENVIRONMENT=Development deberta-promptguard
```

---

## 📊 Compare: Docker Run vs Docker Compose

| Feature | Docker Run | Docker Compose |
|---------|------------|----------------|
| **Command Length** | Long, many flags | Short, one command |
| **Configuration** | Command-line args | YAML file |
| **Multi-Container** | Manual networking | Automatic |
| **Updates** | Re-type command | `docker-compose up -d` |
| **Version Control** | Hard to track | YAML in git |
| **Best For** | Single container, quick tests | Production, multi-service |

---

## 🎯 Recommended Approach

**For Development:**
```bash
# Use docker-compose (easier)
docker-compose up -d
```

**For Production:**
```bash
# Use docker run with explicit settings
docker run -d \
  --name deberta-promptguard \
  -p 5158:8080 \
  -v /data/models:/app/models:ro \
  -e ASPNETCORE_ENVIRONMENT=Production \
  --restart always \
  --memory="2g" \
  --log-driver json-file \
  deberta-promptguard:latest
```

**For Quick Tests:**
```bash
# Run without detached mode to see output
docker run -it --rm \
  -p 5158:8080 \
  -v $(pwd)/models:/app/models:ro \
  deberta-promptguard:latest
```

---

## 🌐 Access Your API

After running the container:

- **Swagger UI:** http://localhost:5158/swagger
- **Health Check:** http://localhost:5158/health
- **API Endpoint:** http://localhost:5158/api/detect
- **Model Status:** http://localhost:5158/api/model/status
- **Logs:** http://localhost:5158/api/logs

---

## 💡 Pro Tips

1. **Use `--rm` for testing:** Container auto-deletes when stopped
   ```bash
   docker run -it --rm -p 5158:8080 deberta-promptguard
   ```

2. **Use `.env` file:** Store environment variables
   ```bash
   docker run -d --env-file .env deberta-promptguard
   ```

3. **Export container config:** Generate docker-compose from running container
   ```bash
   docker inspect deberta-promptguard
   ```

4. **Shell into container:** Debug issues
   ```bash
   docker exec -it deberta-promptguard bash
   ```

5. **Copy files from container:**
   ```bash
   docker cp deberta-promptguard:/app/logs ./container-logs
   ```

---

## 🆘 Common Issues

### **"Address already in use"**
```bash
# Change port
docker run -d -p 5159:8080 ...
```

### **"Cannot start container"**
```bash
# Check logs
docker logs deberta-promptguard

# Run interactively
docker run -it --rm deberta-promptguard
```

### **"Volume not found"**
```bash
# Use absolute path
docker run -d -v C:/full/path/to/models:/app/models:ro ...
```

---

## ✅ Verification Steps

After running:

```bash
# 1. Container is running
docker ps | grep deberta-promptguard
# Should show: STATUS = Up X seconds (healthy)

# 2. Health is good
curl http://localhost:5158/health
# Should return: {"status":"healthy",...}

# 3. Logs are clean
docker logs deberta-promptguard
# Should show: "Now listening on: http://[::]:8080"

# 4. Models loaded
curl http://localhost:5158/api/model/status
# Should return: {"modelLoaded":true,...}
```

---

**You're now running the API with Docker!** 🎉


