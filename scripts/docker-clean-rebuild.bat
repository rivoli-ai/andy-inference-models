@echo off
REM Clean rebuild script for Docker Compose with model downloading

echo ==========================================
echo Docker Compose Clean Rebuild
echo ==========================================
echo.

REM Stop and remove containers
echo [1/5] Stopping and removing containers...
docker-compose down

REM Remove old images
echo [2/5] Removing old images...
docker rmi inference-service tokenizer-service 2>nul || echo No old images to remove

REM Build with no cache to ensure fresh download
echo [3/5] Building images (this may take 5-10 minutes on first run)...
echo       Downloading models from HuggingFace...
docker-compose build --no-cache --build-arg DOWNLOAD_MODELS=true

REM Check if build succeeded
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Build failed! Check the error messages above.
    exit /b 1
)

echo [4/5] Starting services...
docker-compose up -d

echo [5/5] Waiting for services to be healthy...
timeout /t 15 /nobreak >nul

REM Verify models are loaded
echo.
echo ==========================================
echo Verifying Models...
echo ==========================================

REM Check if API is responding
curl -s http://localhost:5158/api/models >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo [OK] API is responding
    echo.
    echo Available models:
    curl -s http://localhost:5158/api/models
) else (
    echo [ERROR] API not responding yet. Check logs:
    echo    docker-compose logs promptguard-api
)

echo.
echo ==========================================
echo Services Status
echo ==========================================
docker-compose ps

echo.
echo To view logs: docker-compose logs -f
echo To stop: docker-compose down

