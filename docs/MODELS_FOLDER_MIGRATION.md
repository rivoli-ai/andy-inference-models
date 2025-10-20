# Models Folder Structure Migration Guide

This guide explains how to organize your model files into model-specific folders for better management and multi-model support.

## Overview

The new folder structure organizes each model in its own directory, making it easier to:
- Manage multiple models
- Version models independently
- Deploy and update models separately
- Maintain cleaner organization

## Recommended Folder Structure

### New Structure (Organized)
```
models/
├── deberta-v3-base-prompt-injection-v2/
│   ├── model.onnx
│   ├── tokenizer.json
│   ├── config.json
│   ├── special_tokens_map.json
│   ├── tokenizer_config.json
│   ├── added_tokens.json
│   └── spm.model
└── deberta-v3-large-prompt-injection/
    ├── model.onnx
    ├── tokenizer.json
    ├── config.json
    └── ...
```

### Old Structure (Flat)
```
models/
├── model.onnx
├── tokenizer.json
├── config.json
├── special_tokens_map.json
├── tokenizer_config.json
├── added_tokens.json
└── spm.model
```

## Migration Steps

### Option 1: Reorganize Existing Files

If you want to migrate your existing model files:

#### Windows (PowerShell)
```powershell
# Navigate to project root
cd C:\dev\Projects\rivoli\andy-inference-models

# Create new model folder
mkdir models\deberta-v3-base-prompt-injection-v2

# Move all model files to new folder
Move-Item models\*.onnx models\deberta-v3-base-prompt-injection-v2\
Move-Item models\*.json models\deberta-v3-base-prompt-injection-v2\
Move-Item models\*.model models\deberta-v3-base-prompt-injection-v2\
```

#### Linux/Mac (Bash)
```bash
# Navigate to project root
cd /path/to/andy-inference-models

# Create new model folder
mkdir -p models/deberta-v3-base-prompt-injection-v2

# Move all model files to new folder
mv models/*.onnx models/deberta-v3-base-prompt-injection-v2/
mv models/*.json models/deberta-v3-base-prompt-injection-v2/
mv models/*.model models/deberta-v3-base-prompt-injection-v2/
```

### Option 2: Keep Backward Compatibility

If you want to keep the current structure but prepare for multiple models:

1. Create a symbolic link or copy for backward compatibility:

#### Windows (PowerShell - Run as Administrator)
```powershell
# Create symbolic link to maintain backward compatibility
New-Item -ItemType SymbolicLink -Path "models\deberta-v3-base-prompt-injection-v2" -Target "models"
```

#### Linux/Mac
```bash
# Create symbolic link
ln -s . models/deberta-v3-base-prompt-injection-v2
```

## Configuration Update

After reorganizing, update your `appsettings.json`:

```json
{
  "AvailableModels": [
    {
      "Id": "deberta-v3-base-prompt-injection-v2",
      "Name": "DeBERTa v3 Base Prompt Injection v2",
      "Description": "DeBERTa v3 base model fine-tuned for prompt injection detection",
      "Version": "v2",
      "Labels": [ "SAFE", "INJECTION" ],
      "MaxSequenceLength": 512,
      "Architecture": "deberta-v3-base",
      "Status": "available",
      "ModelFolder": "models/deberta-v3-base-prompt-injection-v2"
    }
  ]
}
```

### Optional: Override Specific Paths

If your files have different names or locations:

```json
{
  "AvailableModels": [
    {
      "Id": "deberta-v3-base-prompt-injection-v2",
      "Name": "DeBERTa v3 Base Prompt Injection v2",
      "Description": "...",
      "Version": "v2",
      "Labels": [ "SAFE", "INJECTION" ],
      "MaxSequenceLength": 512,
      "Architecture": "deberta-v3-base",
      "Status": "available",
      "ModelFolder": "models/deberta-v3-base-prompt-injection-v2",
      "ModelPath": "models/deberta-v3-base-prompt-injection-v2/custom-model.onnx",
      "TokenizerPath": "models/deberta-v3-base-prompt-injection-v2/custom-tokenizer.json"
    }
  ]
}
```

## Default Behavior

If you don't specify paths, the system will use these defaults:
- **ModelFolder**: `models/{ModelId}`
- **ModelPath**: `{ModelFolder}/model.onnx`
- **TokenizerPath**: `{ModelFolder}/tokenizer.json`

## Adding a Second Model

Once migrated, adding a new model is straightforward:

1. **Create model folder**:
```bash
mkdir models/deberta-v3-large-prompt-injection
```

2. **Add model files**:
```
models/deberta-v3-large-prompt-injection/
├── model.onnx
├── tokenizer.json
└── config.json
```

3. **Update configuration**:
```json
{
  "AvailableModels": [
    {
      "Id": "deberta-v3-base-prompt-injection-v2",
      ...
    },
    {
      "Id": "deberta-v3-large-prompt-injection",
      "Name": "DeBERTa v3 Large Prompt Injection",
      "Description": "Larger model with improved accuracy",
      "Version": "v1",
      "Labels": [ "SAFE", "INJECTION" ],
      "MaxSequenceLength": 512,
      "Architecture": "deberta-v3-large",
      "Status": "available",
      "ModelFolder": "models/deberta-v3-large-prompt-injection"
    }
  ]
}
```

4. **Restart the API**

Both models will now be available:
- `GET /deberta-v3-base-prompt-injection-v2/health`
- `GET /deberta-v3-large-prompt-injection/health`

## Required Files per Model

Each model folder should contain these files:

### Minimal Required Files
- `model.onnx` - The ONNX model file
- `tokenizer.json` - Tokenizer configuration

### Optional Files (Recommended)
- `config.json` - Model configuration
- `special_tokens_map.json` - Special tokens mapping
- `tokenizer_config.json` - Tokenizer configuration
- `added_tokens.json` - Additional tokens
- `spm.model` - SentencePiece model (if using)
- `vocab.txt` - Vocabulary file (if using)

## Verification

After migration, verify everything works:

1. **Check model list**:
```bash
curl http://localhost:5000/api/models
```

2. **Test health endpoint**:
```bash
curl http://localhost:5000/deberta-v3-base-prompt-injection-v2/health
```

3. **Test detection**:
```bash
curl -X POST http://localhost:5000/deberta-v3-base-prompt-injection-v2/api/detect \
  -H "Content-Type: application/json" \
  -d '{"text": "Test input"}'
```

## Docker Considerations

If using Docker, update your `Dockerfile` to copy model folders:

```dockerfile
# Copy model folders
COPY models/ /app/models/

# Or copy specific model folder
COPY models/deberta-v3-base-prompt-injection-v2/ /app/models/deberta-v3-base-prompt-injection-v2/
```

Update `docker-compose.yml` volume mounts:
```yaml
volumes:
  - ./models:/app/models:ro
```

## Troubleshooting

### Model not found error
- Verify the `ModelFolder` path in configuration matches the actual folder
- Check that model files exist in the specified folder
- Ensure file names match (case-sensitive on Linux)

### Path not found error
- Check that paths are relative to the application root
- In Development, you might need `../../models/...` depending on working directory
- Verify file permissions (especially in Docker)

### Multiple models not appearing
- Ensure each model has a unique `Id`
- Check JSON syntax in `appsettings.json`
- Look for errors in application logs
- Restart the application after configuration changes

## Rollback

If you need to revert to the old structure:

1. Move files back to `models/` root
2. Remove the `ModelFolder` property from configuration
3. Update `ModelConfiguration.ModelPath` and `TokenizerPath` to point to root files

## Best Practices

1. **Use consistent naming**: Model folder names should match the model ID
2. **Version your folders**: Include version in folder name (`-v2`, `-v1`)
3. **Document models**: Add a README.md in each model folder
4. **Keep backups**: Before migrating, backup your models folder
5. **Test after migration**: Verify all endpoints work with the new structure

## Example: Complete Migration

Here's a complete example of migrating and adding a second model:

### Step 1: Reorganize
```bash
# Create new structure
mkdir models/deberta-v3-base-prompt-injection-v2
mv models/*.{onnx,json,model} models/deberta-v3-base-prompt-injection-v2/

# Add second model
mkdir models/deberta-v3-large-prompt-injection
# (Copy new model files here)
```

### Step 2: Update Configuration
```json
{
  "AvailableModels": [
    {
      "Id": "deberta-v3-base-prompt-injection-v2",
      "Name": "DeBERTa v3 Base v2",
      "ModelFolder": "models/deberta-v3-base-prompt-injection-v2",
      ...
    },
    {
      "Id": "deberta-v3-large-prompt-injection",
      "Name": "DeBERTa v3 Large",
      "ModelFolder": "models/deberta-v3-large-prompt-injection",
      ...
    }
  ]
}
```

### Step 3: Test
```bash
# List models
curl http://localhost:5000/api/models

# Test each model
curl http://localhost:5000/deberta-v3-base-prompt-injection-v2/health
curl http://localhost:5000/deberta-v3-large-prompt-injection/health
```

## Support

For issues or questions:
- Check application logs for detailed error messages
- Verify file paths are correct
- Ensure all required files are present in model folders
- See `MODELS_CONFIGURATION.md` for configuration details

