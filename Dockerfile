# Build argument to control whether to download models
ARG DOWNLOAD_MODELS=true

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY andy-inference-models.sln .
COPY src/InferenceModel.ML/InferenceModel.ML.csproj src/InferenceModel.ML/
COPY src/InferenceModel.Api/InferenceModel.Api.csproj src/InferenceModel.Api/

# Restore dependencies
RUN dotnet restore

# Copy all source code
COPY src/ src/

# Build the application
WORKDIR /src/src/InferenceModel.Api
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Model download stage - using lightweight Python image (not in final image!)
FROM python:3.11-slim AS model-downloader
ARG DOWNLOAD_MODELS

# Create base models directory and copy existing models from repository (if available)
RUN mkdir -p /models
COPY models/ /models/

# Copy all conversion scripts and configuration
WORKDIR /workspace
COPY scripts/ ./scripts/
COPY config/ ./config/

# Install Python dependencies conditionally and download missing models
RUN chmod +x scripts/download_missing_models.py && \
    # Quick check: do we need to download anything?
    if python scripts/download_missing_models.py --models-dir /models --check-only 2>/dev/null | grep -q "Models to download: 0"; then \
        echo "✓ All models present, skipping downloads and dependencies installation"; \
    else \
        echo "Installing Python dependencies for model conversion..."; \
        pip install --no-cache-dir torch --index-url https://download.pytorch.org/whl/cpu; \
        pip install --no-cache-dir transformers "optimum[onnxruntime]" onnx onnxscript; \
        echo "Downloading missing models..."; \
        python scripts/download_missing_models.py --models-dir /models || echo "⚠ Some models failed to download"; \
    fi && \
    echo "" && \
    echo "=== Final Model Status ===" && \
    find /models -name "*.onnx" -exec ls -lh {} \; 2>/dev/null || echo "No ONNX models found"

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && \
    apt-get install -y curl && \
    rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=publish /app/publish .

# Copy configuration files
COPY config/ ./config/

# Copy all models from downloader stage (includes pre-existing and downloaded models)
COPY --from=model-downloader /models/ ./models/

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ModelsConfigPath=/app/config/models.json
ENV TokenizerServiceUrl=http://tokenizer-service:8000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Run the application
ENTRYPOINT ["dotnet", "InferenceModel.Api.dll"]


