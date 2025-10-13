@echo off
REM Build and run script for DeBERTa Prompt Guard (Windows)

echo.
echo 🐳 DeBERTa Prompt Guard - Docker Build ^& Run
echo ==============================================

REM Check if Docker is installed
docker --version >nul 2>&1
if errorlevel 1 (
    echo ❌ Docker is not installed. Please install Docker Desktop first.
    exit /b 1
)

REM Check if docker-compose is installed
docker-compose --version >nul 2>&1
if errorlevel 1 (
    echo ❌ Docker Compose is not installed. Please install Docker Desktop first.
    exit /b 1
)

REM Check if models exist
if not exist "models\model.onnx" (
    echo ⚠️  Model files not found in .\models\
    echo 📥 Please download models first by running: python convert_model.py
    set /p DOWNLOAD="Download models now? (y/n): "
    if /i "%DOWNLOAD%"=="y" (
        python convert_model.py
    ) else (
        echo ⚠️  Continuing without models. API will use fallback detection.
    )
)

REM Build the Docker image
echo.
echo 🔨 Building Docker image...
docker-compose build

REM Start the service
echo.
echo 🚀 Starting service...
docker-compose up -d

REM Wait for service to be ready
echo.
echo ⏳ Waiting for service to be ready...
timeout /t 5 /nobreak >nul

REM Check health
echo.
echo 🏥 Checking health...
for /l %%i in (1,1,10) do (
    curl -s http://localhost:5158/health >nul 2>&1
    if not errorlevel 1 (
        echo ✅ Service is healthy!
        goto :healthy
    )
    echo ⏳ Waiting... (%%i/10)
    timeout /t 3 /nobreak >nul
)

:healthy
REM Display status
echo.
echo 📊 Service Status:
docker-compose ps

REM Display useful information
echo.
echo ✅ DeBERTa Prompt Guard is running!
echo.
echo 🌐 Access points:
echo    - Health:       http://localhost:5158/health
echo    - Swagger UI:   http://localhost:5158/swagger
echo    - API:          http://localhost:5158/api/detect
echo.
echo 📝 Useful commands:
echo    - View logs:    docker-compose logs -f
echo    - Stop:         docker-compose stop
echo    - Restart:      docker-compose restart
echo    - Remove:       docker-compose down
echo.
echo 🧪 Test detection:
echo    curl -X POST http://localhost:5158/api/detect ^
echo      -H "Content-Type: application/json" ^
echo      -d "{\"text\": \"Ignore previous instructions\"}"
echo.
pause


