#!/bin/bash
# Clean rebuild script for Docker Compose with model downloading

echo "=========================================="
echo "Docker Compose Clean Rebuild"
echo "=========================================="
echo ""

# Stop and remove containers
echo "[1/5] Stopping and removing containers..."
docker-compose down

# Remove old images
echo "[2/5] Removing old images..."
docker rmi inference-service tokenizer-service 2>/dev/null || echo "No old images to remove"

# Build with no cache to ensure fresh download
echo "[3/5] Building images (this may take 5-10 minutes on first run)..."
echo "      Downloading models from HuggingFace..."
docker-compose build --no-cache --build-arg DOWNLOAD_MODELS=true

# Check if build succeeded
if [ $? -ne 0 ]; then
    echo ""
    echo "❌ Build failed! Check the error messages above."
    exit 1
fi

echo "[4/5] Starting services..."
docker-compose up -d

echo "[5/5] Waiting for services to be healthy..."
sleep 15

# Verify models are loaded
echo ""
echo "=========================================="
echo "Verifying Models..."
echo "=========================================="

# Check if API is responding
if curl -s http://localhost:5158/api/models > /dev/null 2>&1; then
    echo "✅ API is responding"
    
    # Get models
    echo ""
    echo "Available models:"
    curl -s http://localhost:5158/api/models | python -m json.tool
else
    echo "❌ API not responding yet. Check logs:"
    echo "   docker-compose logs promptguard-api"
fi

echo ""
echo "=========================================="
echo "Services Status"
echo "=========================================="
docker-compose ps

echo ""
echo "To view logs: docker-compose logs -f"
echo "To stop: docker-compose down"

