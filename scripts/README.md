# Model Conversion Scripts

This directory contains scripts for downloading and converting ML models to ONNX format for use with the Inference API.

## 🚀 How It Works

The Docker build process uses `download_missing_models.py` to **automatically discover and download** only the models that are missing. This means:

- ✅ No hardcoded model lists in Dockerfile
- ✅ Automatically detects missing models
- ✅ Skips downloads if models already exist
- ✅ Easy to add new models - just create a conversion script!

## 📝 Adding a New Model

Adding a new model is now **incredibly simple** - just edit one JSON file!

### 1. ~~Create a Conversion Script~~ (No longer needed!)

**You don't need to write any Python code!** The generic `convert_to_onnx.py` script handles all HuggingFace models automatically.

### 2. Just Edit config/models.json

**Simply edit `config/models.json`** and add your model configuration:

```json
{
  "models": [
    {
      "id": "your-model",
      "name": "Your Model Display Name",
      "huggingface_model": "author/model-name-on-huggingface",
      "output_dir": "your-model-name",
      "check_files": ["model.onnx", "tokenizer.json"],
      "task_type": "sequence-classification",
      "max_length": 512,
      "opset_version": 14,
      "required": true,
      "description": "Brief description of your model"
    }
  ],
  "config": {
    "conversion_script": "convert_to_onnx.py"
  }
}
```

**Model Field Descriptions:**
- `id`: Unique identifier for the model (kebab-case) - **Required**
- `name`: Human-readable display name - **Required**
- `huggingface_model`: HuggingFace model ID (e.g., "bert-base-uncased") - **Required**
- `output_dir`: Directory name where model files will be stored - **Required**
- `check_files`: List of files to verify model exists - Optional (default: `["model.onnx"]`)
- `task_type`: Model task (sequence-classification, etc.) - Optional (default: "sequence-classification")
- `max_length`: Maximum sequence length - Optional (default: 512)
- `opset_version`: ONNX opset version - Optional (default: 14)
- `required`: Whether this model is mandatory - Optional (default: false)
- `description`: Brief description of the model's purpose - Optional

**Global Config:**
- `conversion_script`: Conversion script to use for all models (default: `convert_to_onnx.py`)

### 3. That's It - Really!

The system will automatically:
- ✅ Read configuration from `config/models.json`
- ✅ Detect if your model is missing
- ✅ Download from HuggingFace using the generic converter
- ✅ Convert to ONNX format
- ✅ Move files to the correct location
- ✅ Include the model in the final Docker image

**No Python code needed! Just edit `config/models.json`!** 🎉

Example: Adding BERT Base model
```json
{
  "id": "bert-base",
  "name": "BERT Base Uncased",
  "huggingface_model": "bert-base-uncased",
  "output_dir": "bert-base-uncased",
  "check_files": ["model.onnx", "tokenizer.json"],
  "description": "BERT base model for general NLP tasks"
}
```

## 🔧 Available Scripts

### Generic Conversion Script

- **`convert_to_onnx.py`** - Universal HuggingFace to ONNX converter
  - Handles any HuggingFace model automatically
  - Reads configuration from `config/models.json`
  - Can be used standalone with command-line arguments

### Master Download Script

- **`download_missing_models.py`** - Orchestrates model downloads
  - Reads `config/models.json` for configuration
  - Checks which models are missing
  - Calls `convert_to_onnx.py` with appropriate parameters
  - Handles file movement and verification

### Configuration

- **`config/models.json`** - **Single source of truth for all models**
  - Defines all available models
  - Specifies HuggingFace model IDs
  - Configures conversion parameters
  - **Edit this file to add new models!**

### Utility Scripts

- **`build-and-run.sh`** / **`build-and-run.bat`** - Build Docker image and run container
  - Automates the build and run process
  - Cross-platform (Linux/Mac and Windows)

#### Usage:

```bash
# Download all missing models
python3 download_missing_models.py --models-dir /models

# Force re-download all models
python3 download_missing_models.py --force

# Check which models are missing (no download)
python3 download_missing_models.py --check-only

# Use environment variable to force download
DOWNLOAD_MODELS=true python3 download_missing_models.py
```

## 🐳 Docker Build Behavior

### Standard Dockerfile
- Copies existing models from `models/` directory
- Checks which models are missing
- Only installs Python dependencies if downloads needed
- Downloads only missing models
- Fast builds when models exist!

### Dockerfile.with-models
- Always downloads all models (uses `--force`)
- Ensures fresh models in the image
- Use when you want to guarantee latest model versions

## 📦 Build Arguments

```bash
# Standard build - use existing models, download missing ones
docker build -t myimage .

# Force download all models
docker build -t myimage --build-arg DOWNLOAD_MODELS=true .

# Skip downloads (only works if all models exist)
docker build -t myimage --build-arg DOWNLOAD_MODELS=false .
```

## 📄 config/models.json Configuration

The `config/models.json` file is the single source of truth for model configuration:

```json
{
  "models": [
    {
      "id": "unique-model-id",
      "name": "Human Readable Name",
      "conversion_script": "convert_script.py",
      "output_dir": "model-directory-name",
      "check_files": ["model.onnx", "tokenizer.json"],
      "required": true,
      "description": "Model purpose and details"
    }
  ],
  "config": {
    "models_base_dir": "/models",
    "python_requirements": [
      "torch --index-url https://download.pytorch.org/whl/cpu",
      "transformers",
      "optimum[onnxruntime]",
      "onnx"
    ]
  }
}
```

### Model Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `id` | string | ✅ Yes | Unique identifier (kebab-case) |
| `name` | string | ✅ Yes | Display name for logging |
| `conversion_script` | string | ✅ Yes | Python script filename |
| `output_dir` | string | ✅ Yes | Directory name in models/ |
| `check_files` | array | No | Files to verify (default: `["model.onnx"]`) |
| `required` | boolean | No | Is this model mandatory? |
| `description` | string | No | Model purpose documentation |

### Global Config

- `models_base_dir`: Base directory for all models (default: `/models`)
- `python_requirements`: pip packages needed for conversion (for future use)

## 🎯 Benefits

1. **Declarative**: All model config in JSON, no code changes
2. **Scalable**: Add models by editing JSON only
3. **Fast**: Skips downloads when models exist
4. **Flexible**: Works with pre-existing models or downloads on-demand
5. **Maintainable**: Separation of config and logic
6. **Debuggable**: Clear output shows what's happening
7. **Versionable**: JSON file can be versioned and reviewed easily

## 📂 Model Directory Structure

Expected structure after conversion:

```
models/
├── deberta-v3-base-prompt-injection-v2/
│   ├── model.onnx
│   ├── tokenizer.json
│   ├── config.json
│   └── ...
├── graphcodebert-solidity-vulnerability/
│   ├── model.onnx
│   ├── tokenizer.json
│   ├── config.json
│   └── ...
└── your-model-name/
    ├── model.onnx
    ├── tokenizer.json
    └── config.json
```

## 🔍 Troubleshooting

**Models not being copied into Docker image?**
- Check `.dockerignore` - make sure `**/models/*` is commented out

**Script fails during Docker build?**
- Run the script locally first to test
- Check HuggingFace model name is correct
- Ensure output directory matches `MODEL_CONFIGS`

**Want to test locally?**
```bash
python3 scripts/download_missing_models.py --models-dir ./models
```


