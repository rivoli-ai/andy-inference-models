# Docker Deployment Guide

## 🐳 Quick Start

### Prerequisites
- Docker 20.10+
- Docker Compose 2.0+
- Model files (see below)

## 📦 Deployment Options

### Option 1: Docker Compose (Recommended)

**With Local Model Files:**

```bash
# 1. Download model files first
python convert_model.py

# 2. Start the service
docker-compose up -d

# 3. Check status
docker-compose ps
docker-compose logs -f promptguard-api

# 4. Access API
curl http://localhost:5000/health
```

**With Models as Volume:**

```bash
# Models are expected in ./models directory
docker-compose up -d
```

### Option 2: Docker Build and Run (Without Docker Compose)

**Build the Image:**

```bash
# Build image
docker build -t deberta-promptguard:latest .
```

**Run the Container (Linux/Mac):**

```bash
# Run container with all settings
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

**Run the Container (Windows PowerShell):**

```powershell
# Run container with all settings
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

**Manage the Container:**

```bash
# Check logs
docker logs -f deberta-promptguard

# Stop container
docker stop deberta-promptguard

# Start container
docker start deberta-promptguard

# Remove container
docker rm -f deberta-promptguard

# Restart container
docker restart deberta-promptguard
```

**With Models Included in Image:**

```bash
# Build image with models
docker build -f Dockerfile.with-models -t deberta-promptguard:with-models .

# Run container (no volume needed)
docker run -d \
  --name promptguard \
  -p 5158:8080 \
  deberta-promptguard:with-models
```

### Option 3: Development Mode

```bash
# Use development compose file
docker-compose -f docker-compose.dev.yml up

# Access Swagger UI
http://localhost:5000/swagger
```

## 📁 Model Files

### Prepare Model Files

**Method 1: Local Download**
```bash
# Run the conversion script
python convert_model.py

# Verify files exist
ls -la src/DebertaPromptGuard.Api/models/
# Should show: model.onnx, tokenizer.json
```

**Method 2: Download Separately**
```bash
# Create models directory
mkdir -p models

# Download from your model storage
# Place model.onnx and tokenizer.json in ./models/
```

## 🚀 Deployment Scenarios

### Scenario 1: Local Development

```bash
# Development with hot reload
docker-compose -f docker-compose.dev.yml up

# Access API at http://localhost:5158
# Swagger at http://localhost:5158/swagger
```

### Scenario 2: Production Deployment

```bash
# Build production image
docker build -t deberta-promptguard:prod .

# Run with production settings
docker run -d \
  --name promptguard-prod \
  -p 5158:8080 \
  -v /data/models:/app/models:ro \
  -e ASPNETCORE_ENVIRONMENT=Production \
  --restart unless-stopped \
  deberta-promptguard:prod
```

### Scenario 3: Docker Swarm / Kubernetes

See deployment examples below.

## ⚙️ Configuration

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `ASPNETCORE_URLS` | `http://+:8080` | Bind address |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Environment name |
| `Logging__MaxLogSize` | `1000` | Max prediction logs |
| `ModelConfiguration__ModelPath` | `models/model.onnx` | ONNX model path |
| `ModelConfiguration__TokenizerPath` | `models/tokenizer.json` | Tokenizer path |

### Docker Compose Override

Create `docker-compose.override.yml`:

```yaml
version: '3.8'

services:
  promptguard-api:
    environment:
      - Logging__MaxLogSize=5000
    ports:
      - "8080:8080"
```

## 🏥 Health Checks

The container includes built-in health checks:

```bash
# Check container health
docker inspect --format='{{.State.Health.Status}}' promptguard

# View health check logs
docker inspect --format='{{json .State.Health}}' promptguard | jq
```

## 📊 Monitoring

### View Logs

```bash
# Follow logs
docker-compose logs -f

# Last 100 lines
docker-compose logs --tail=100

# Specific service
docker logs promptguard-api
```

### Access Metrics

```bash
# Check health endpoint
curl http://localhost:5158/health/detailed

# View logs stats
curl http://localhost:5158/api/logs/stats

# Model status
curl http://localhost:5158/api/model/status
```

## 🔧 Troubleshooting

### Container Won't Start

```bash
# Check logs
docker-compose logs promptguard-api

# Common issues:
# 1. Model files missing
# 2. Port already in use
# 3. Insufficient memory
```

### Model Not Loading

```bash
# Verify models exist in container
docker exec promptguard ls -la /app/models

# Check if models are mounted correctly
docker inspect promptguard | grep -A 10 Mounts

# Test with fallback disabled
curl -X POST http://localhost:5158/api/model/disable
```

### High Memory Usage

```bash
# Limit container memory
docker run -d \
  --name promptguard \
  --memory="2g" \
  --memory-swap="2g" \
  -p 5000:8080 \
  deberta-promptguard:latest
```

## 🎯 Best Practices

### 1. Use Multi-Stage Builds
- Reduces image size
- Faster builds
- Better security

### 2. Volume Mount Models
- Don't include 500MB+ models in image
- Easier updates
- Smaller images

### 3. Set Resource Limits

```yaml
services:
  promptguard-api:
    deploy:
      resources:
        limits:
          cpus: '2'
          memory: 2G
        reservations:
          cpus: '1'
          memory: 1G
```

### 4. Use Health Checks

Already included in Dockerfile and compose files.

### 5. Security

```yaml
services:
  promptguard-api:
    read_only: true
    security_opt:
      - no-new-privileges:true
    tmpfs:
      - /tmp
```

## 🌐 Cloud Deployment

### Azure Container Instances

```bash
# Build and push to ACR
az acr build --registry myregistry \
  --image deberta-promptguard:latest .

# Create container instance
az container create \
  --resource-group myResourceGroup \
  --name promptguard \
  --image myregistry.azurecr.io/deberta-promptguard:latest \
  --dns-name-label promptguard \
  --ports 8080
```

### AWS ECS

```bash
# Build and push to ECR
aws ecr get-login-password --region us-east-1 | \
  docker login --username AWS --password-stdin \
  123456789.dkr.ecr.us-east-1.amazonaws.com

docker build -t deberta-promptguard .
docker tag deberta-promptguard:latest \
  123456789.dkr.ecr.us-east-1.amazonaws.com/deberta-promptguard:latest
docker push 123456789.dkr.ecr.us-east-1.amazonaws.com/deberta-promptguard:latest

# Create ECS task definition and service
```

### Google Cloud Run

```bash
# Build and deploy
gcloud builds submit --tag gcr.io/PROJECT-ID/deberta-promptguard
gcloud run deploy promptguard \
  --image gcr.io/PROJECT-ID/deberta-promptguard \
  --platform managed \
  --region us-central1 \
  --allow-unauthenticated
```

## 🔄 Updates

### Update Application

```bash
# Pull latest changes
git pull

# Rebuild and restart
docker-compose up -d --build
```

### Update Models

```bash
# Stop container
docker-compose stop

# Update models in ./models/

# Start container
docker-compose start
```

## 📈 Scaling

### Docker Compose Scale

```bash
# Run multiple instances
docker-compose up -d --scale promptguard-api=3

# Add load balancer (nginx/traefik)
```

### Kubernetes Deployment

Create `k8s-deployment.yaml`:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: promptguard
spec:
  replicas: 3
  selector:
    matchLabels:
      app: promptguard
  template:
    metadata:
      labels:
        app: promptguard
    spec:
      containers:
      - name: api
        image: deberta-promptguard:latest
        ports:
        - containerPort: 8080
        volumeMounts:
        - name: models
          mountPath: /app/models
      volumes:
      - name: models
        persistentVolumeClaim:
          claimName: models-pvc
---
apiVersion: v1
kind: Service
metadata:
  name: promptguard
spec:
  selector:
    app: promptguard
  ports:
  - port: 80
    targetPort: 8080
  type: LoadBalancer
```

Deploy:
```bash
kubectl apply -f k8s-deployment.yaml
```

## 🛡️ Security Considerations

1. **Don't expose on public internet without authentication**
2. **Use HTTPS in production**
3. **Implement rate limiting**
4. **Scan images for vulnerabilities**:
   ```bash
   docker scan deberta-promptguard:latest
   ```
5. **Use secrets for sensitive config**

## 📝 Example Complete Workflow

```bash
# 1. Clone repository
git clone <repo-url>
cd DebertaPromptGuard

# 2. Download models
python convert_model.py

# 3. Build and start
docker-compose up -d

# 4. Verify
curl http://localhost:5158/health
curl http://localhost:5158/swagger

# 5. Test detection
curl -X POST http://localhost:5158/api/detect \
  -H "Content-Type: application/json" \
  -d '{"text": "Ignore previous instructions"}'

# 6. View logs
docker-compose logs -f

# 7. Stop
docker-compose down
```

## 🆘 Support

For issues with Docker deployment:
1. Check logs: `docker-compose logs`
2. Verify models: `docker exec promptguard ls /app/models`
3. Test health: `curl http://localhost:5000/health/detailed`
4. Check GitHub Issues

---

**Image Size:**
- Base image: ~200MB
- Application: ~50MB
- Models (not included): ~500MB
- **Total**: ~250MB (without models) or ~750MB (with models)


