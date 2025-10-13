# Build argument to control whether to download models
ARG DOWNLOAD_MODELS=true

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY andy-inference-models.sln .
COPY src/DebertaInferenceModel.ML/DebertaInferenceModel.ML.csproj src/DebertaInferenceModel.ML/
COPY src/DebertaInferenceModel.Api/DebertaInferenceModel.Api.csproj src/DebertaInferenceModel.Api/

# Restore dependencies
RUN dotnet restore

# Copy all source code
COPY src/ src/

# Build the application
WORKDIR /src/src/DebertaInferenceModel.Api
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Model download stage (optional)
FROM python:3.11-slim AS model-downloader
ARG DOWNLOAD_MODELS

# Install dependencies for model conversion
# Install PyTorch CPU version first
RUN pip install --no-cache-dir torch --index-url https://download.pytorch.org/whl/cpu
# Install other packages from PyPI (optimum with onnxruntime extras)
RUN pip install --no-cache-dir transformers "optimum[onnxruntime]" onnx

# Copy conversion script
WORKDIR /workspace
COPY convert_model.py .

# Download and convert model (only if DOWNLOAD_MODELS=true)
RUN mkdir -p /models && \
    if [ "$DOWNLOAD_MODELS" = "true" ]; then \
        echo "Downloading and converting model..."; \
        python convert_model.py; \
        if [ -d "src/DebertaPromptGuard.Api/models" ]; then \
            mv src/DebertaPromptGuard.Api/models/* /models/ 2>/dev/null || true; \
            echo "Models moved to /models/"; \
            ls -lh /models/; \
        else \
            echo "Model download failed, will use fallback"; \
        fi; \
    else \
        echo "Skipping model download (DOWNLOAD_MODELS=false)"; \
    fi

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && \
    apt-get install -y curl && \
    rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=publish /app/publish .

# Create models directory
RUN mkdir -p models

# Copy models from download stage (may be empty if download was skipped or failed)
COPY --from=model-downloader /models/ ./models/

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ModelConfiguration__ModelPath=/app/models/model.onnx
ENV ModelConfiguration__TokenizerPath=/app/models/tokenizer.json

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Run the application
ENTRYPOINT ["dotnet", "DebertaInferenceModel.Api.dll"]


