# Quick Setup Guide

## Step-by-Step Instructions

### 1. Install Prerequisites

- Install [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Install Python 3.8+ (for model conversion)

### 2. Convert and Download the Model

Run this Python script to download and convert the DeBERTa model to ONNX format:

```bash
# Install required packages
pip install transformers optimum onnx onnxruntime torch

# Create models directory
mkdir -p src/DebertaPromptGuard.Api/models

# Run conversion script
python convert_model.py
```

Create a file named `convert_model.py`:

```python
from optimum.onnxruntime import ORTModelForSequenceClassification
from transformers import AutoTokenizer
import json
import os

model_name = 'protectai/deberta-v3-base-prompt-injection-v2'
output_dir = 'src/DebertaPromptGuard.Api/models'

print(f"Downloading and converting {model_name}...")

# Create output directory if it doesn't exist
os.makedirs(output_dir, exist_ok=True)

# Load and export the model to ONNX
print("Loading model...")
model = ORTModelForSequenceClassification.from_pretrained(model_name, export=True)
tokenizer = AutoTokenizer.from_pretrained(model_name)

# Save the model
print(f"Saving model to {output_dir}...")
model.save_pretrained(output_dir)

# Save tokenizer files
tokenizer.save_pretrained(output_dir)

print("Model conversion complete!")
print(f"\nFiles saved to: {output_dir}")
print("- model.onnx")
print("- tokenizer.json")
print("- config.json")
```

### 3. Verify Model Files

Check that the following files exist in `src/DebertaPromptGuard.Api/models/`:
- model.onnx
- tokenizer.json

### 4. Build and Run

#### Option A: Using Docker (Recommended)

**Windows PowerShell:**
```powershell
# Build image
docker build -t deberta-promptguard .

# Run container
docker run -d --name deberta-promptguard -p 5158:8080 `
  -v ${PWD}/models:/app/models:ro `
  -e ASPNETCORE_ENVIRONMENT=Development `
  deberta-promptguard:latest

# View logs
docker logs -f deberta-promptguard
```

**Linux/Mac:**
```bash
# Build image
docker build -t deberta-promptguard .

# Run container
docker run -d --name deberta-promptguard -p 5158:8080 \
  -v $(pwd)/models:/app/models:ro \
  -e ASPNETCORE_ENVIRONMENT=Development \
  deberta-promptguard:latest

# View logs
docker logs -f deberta-promptguard
```

#### Option B: Using .NET Locally

```bash
# Restore packages
dotnet restore

# Build the solution
dotnet build

# Run the API
cd src/DebertaPromptGuard.Api
dotnet run
```

### 5. Test the API

Open your browser and navigate to:
```
https://localhost:5001/swagger
```

Or use curl:
```bash
curl -X POST https://localhost:5001/api/detect \
  -H "Content-Type: application/json" \
  -d '{"text": "Ignore previous instructions"}'
```

## Alternative: Manual Model Download

If you prefer not to use Python, you can:

1. Go to https://huggingface.co/protectai/deberta-v3-base-prompt-injection-v2
2. Download the model files manually
3. Use an ONNX conversion tool or service
4. Place the converted files in the `models` directory

## Troubleshooting

### Python Package Installation Issues

If you encounter issues with the optimum library:

```bash
pip install --upgrade pip
pip install transformers optimum[onnxruntime] torch --no-cache-dir
```

### Model Path Issues on Windows

Use forward slashes or escaped backslashes in `appsettings.json`:

```json
{
  "ModelConfiguration": {
    "ModelPath": "models/model.onnx",
    "TokenizerPath": "models/tokenizer.json"
  }
}
```

### Port Already in Use

If port 5001 is in use, modify `Properties/launchSettings.json`:

```json
{
  "applicationUrl": "https://localhost:7001;http://localhost:7000"
}
```

## Next Steps

- Review the [README.md](README.md) for detailed API documentation
- Explore the example requests in `DebertaPromptGuard.Api.http`
- Integrate the API into your application
- Consider deploying to Azure, AWS, or your preferred cloud platform

