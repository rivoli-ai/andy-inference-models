# Models Folder Organization

This document provides a quick overview of the model folder structure and available tools.

## Quick Start

### Current Structure
Your model files should be organized in model-specific folders:

```
models/
└── deberta-v3-base-prompt-injection-v2/
    ├── model.onnx
    ├── tokenizer.json
    ├── config.json
    ├── special_tokens_map.json
    ├── tokenizer_config.json
    ├── added_tokens.json
    └── spm.model
```

### Migration Scripts

If your model files are currently in the `models/` root directory, use these scripts to organize them:

#### Windows (PowerShell)
```powershell
# Dry run (preview changes)
.\migrate-models.ps1 -DryRun

# Perform migration
.\migrate-models.ps1

# Migrate with custom model ID
.\migrate-models.ps1 -ModelId "my-custom-model"
```

#### Linux/Mac (Bash)
```bash
# Make script executable
chmod +x migrate-models.sh

# Dry run (preview changes)
./migrate-models.sh --dry-run

# Perform migration
./migrate-models.sh

# Migrate with custom model ID
./migrate-models.sh my-custom-model
```

## Configuration

After organizing your models, update `appsettings.json`:

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

## Required Files

Each model folder should contain at minimum:
- `model.onnx` - The ONNX model file
- `tokenizer.json` - Tokenizer configuration

Additional recommended files:
- `config.json` - Model configuration
- `special_tokens_map.json` - Special tokens
- `tokenizer_config.json` - Tokenizer settings
- `added_tokens.json` - Additional tokens
- `spm.model` - SentencePiece model (if applicable)

## Adding Multiple Models

To support multiple models:

1. **Create separate folders**:
```
models/
├── deberta-v3-base-prompt-injection-v2/
│   └── ...
├── deberta-v3-large-prompt-injection/
│   └── ...
└── custom-model/
    └── ...
```

2. **Update configuration**:
```json
{
  "AvailableModels": [
    {
      "Id": "deberta-v3-base-prompt-injection-v2",
      "ModelFolder": "models/deberta-v3-base-prompt-injection-v2",
      ...
    },
    {
      "Id": "deberta-v3-large-prompt-injection",
      "ModelFolder": "models/deberta-v3-large-prompt-injection",
      ...
    }
  ]
}
```

3. **Restart the API**

4. **Test each model**:
```bash
curl http://localhost:5000/api/models
curl http://localhost:5000/deberta-v3-base-prompt-injection-v2/health
curl http://localhost:5000/deberta-v3-large-prompt-injection/health
```

## Path Customization

You can override default paths if needed:

```json
{
  "Id": "custom-model",
  "ModelFolder": "custom/path/to/model",
  "ModelPath": "custom/path/to/model/custom-name.onnx",
  "TokenizerPath": "custom/path/to/model/custom-tokenizer.json",
  ...
}
```

## Default Behavior

If paths are not specified:
- **ModelFolder**: Defaults to `models/{ModelId}`
- **ModelPath**: Defaults to `{ModelFolder}/model.onnx`
- **TokenizerPath**: Defaults to `{ModelFolder}/tokenizer.json`

## Documentation

For more detailed information:
- **MODELS_FOLDER_MIGRATION.md** - Complete migration guide
- **MODELS_CONFIGURATION.md** - Configuration reference
- **appsettings.json** - Active configuration

## Verification

After setup, verify everything works:

```bash
# List all models
curl http://localhost:5000/api/models

# Check specific model
curl http://localhost:5000/api/models/deberta-v3-base-prompt-injection-v2

# Health check
curl http://localhost:5000/deberta-v3-base-prompt-injection-v2/health

# Test detection
curl -X POST http://localhost:5000/deberta-v3-base-prompt-injection-v2/api/detect \
  -H "Content-Type: application/json" \
  -d '{"text": "Test input"}'
```

## Troubleshooting

### Model not found
- Check that the folder exists and contains required files
- Verify `ModelFolder` path in configuration
- Check file permissions

### Path errors
- Ensure paths are relative to application root
- Check for typos in configuration
- Verify folder and file names (case-sensitive on Linux)

### Files not loading
- Confirm `model.onnx` and `tokenizer.json` exist
- Check file sizes (corrupt downloads?)
- Review application logs for errors

## Support

For issues:
1. Check application logs
2. Verify folder structure matches configuration
3. Test with `GET /api/models` endpoint
4. See detailed guides in documentation

