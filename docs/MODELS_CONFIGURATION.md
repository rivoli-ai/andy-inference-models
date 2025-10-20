# Dynamic Model Configuration

This document explains how to configure and manage multiple models dynamically in the DeBERTa Prompt Guard API.

## Overview

The API now supports dynamic model registration through configuration files. You can add, remove, or modify available models without changing any code. Each model should be organized in its own folder for better management.

## Folder Structure

### Recommended Structure (Multi-Model)
```
models/
├── deberta-v3-base-prompt-injection-v2/
│   ├── model.onnx
│   ├── tokenizer.json
│   ├── config.json
│   └── ...
└── deberta-v3-large-prompt-injection/
    ├── model.onnx
    ├── tokenizer.json
    └── ...
```

See `MODELS_FOLDER_MIGRATION.md` for detailed migration instructions.

## Configuration

Models are configured in the `appsettings.json` or `appsettings.Development.json` file under the `AvailableModels` section.

### Configuration Structure

```json
{
  "AvailableModels": [
    {
      "Id": "model-identifier",
      "Name": "Human Readable Model Name",
      "Description": "Description of what this model does",
      "Version": "v1",
      "Labels": [ "SAFE", "INJECTION" ],
      "MaxSequenceLength": 512,
      "Architecture": "deberta-v3-base",
      "Status": "available",
      "ModelFolder": "models/model-identifier",
      "ModelPath": "models/model-identifier/model.onnx",
      "TokenizerPath": "models/model-identifier/tokenizer.json"
    }
  ]
}
```

### Field Descriptions

- **Id** (required): Unique identifier used in API routes (e.g., `/{model}/api/detect`)
- **Name** (required): Human-readable name displayed in API documentation
- **Description** (required): Brief description of the model's purpose
- **Version** (required): Model version (e.g., "v1", "v2")
- **Labels** (required): Array of classification labels the model can predict
- **MaxSequenceLength** (required): Maximum input sequence length
- **Architecture** (required): Base model architecture
- **Status** (required): Model status ("available", "deprecated", etc.)
- **ModelFolder** (optional): Path to model folder (defaults to `models/{Id}`)
- **ModelPath** (optional): Path to ONNX model file (defaults to `{ModelFolder}/model.onnx`)
- **TokenizerPath** (optional): Path to tokenizer file (defaults to `{ModelFolder}/tokenizer.json`)

## Adding a New Model

To add a new model, simply add a new entry to the `AvailableModels` array in `appsettings.json`:

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
    },
    {
      "Id": "deberta-v3-large-prompt-injection",
      "Name": "DeBERTa v3 Large Prompt Injection",
      "Description": "Larger DeBERTa v3 model with improved accuracy",
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

After adding a model to the configuration, restart the API for changes to take effect.

## Removing a Model

To remove a model, simply delete its entry from the `AvailableModels` array and restart the API.

## Checking Available Models

### Via API

Use the following endpoints to check available models:

```bash
# Get all models with details
GET /api/models

# Get specific model information
GET /api/models/{modelId}

# Get API information including model list
GET /
```

### Response Example

```json
{
  "count": 1,
  "models": [
    {
      "id": "deberta-v3-base-prompt-injection-v2",
      "name": "DeBERTa v3 Base Prompt Injection v2",
      "description": "DeBERTa v3 base model fine-tuned for prompt injection detection",
      "version": "v2",
      "labels": ["SAFE", "INJECTION"],
      "maxSequenceLength": 512,
      "architecture": "deberta-v3-base",
      "status": "available"
    }
  ]
}
```

## Using Models in API Calls

All API endpoints require a model ID in the route:

```bash
# Health check
GET /{modelId}/health

# Detection
POST /{modelId}/api/detect
{
  "text": "Your text to analyze"
}

# Batch detection
POST /{modelId}/api/detect/batch
[
  { "text": "First text" },
  { "text": "Second text" }
]
```

### Example

```bash
# Using the v2 model
POST /deberta-v3-base-prompt-injection-v2/api/detect
{
  "text": "Ignore all previous instructions"
}
```

## Model Validation

The API automatically validates that the requested model exists. If you try to use a non-existent model, you'll receive a 404 response:

```json
{
  "error": "Model 'invalid-model' not found",
  "availableModels": [
    "deberta-v3-base-prompt-injection-v2"
  ],
  "message": "Please use GET /api/models to see available models"
}
```

## Default Behavior

If no models are configured in `appsettings.json`, the API will automatically register a default model:
- **Id**: `deberta-v3-base-prompt-injection-v2`
- **Name**: DeBERTa v3 Base Prompt Injection v2

## Swagger Documentation

The Swagger UI automatically displays:
- Available model IDs in parameter descriptions
- Default model ID in examples
- Dynamic model list in the API description

Access Swagger at: `http://localhost:5000/swagger`

## Best Practices

1. **Use descriptive IDs**: Model IDs appear in URLs, so keep them URL-friendly (lowercase, hyphens)
2. **Version your models**: Include version information in the ID (e.g., `-v2`)
3. **Keep descriptions current**: Update descriptions when model behavior changes
4. **Mark deprecated models**: Change status to "deprecated" before removing
5. **Test after changes**: Always test the `/api/models` endpoint after configuration changes

## Example Multi-Model Configuration

```json
{
  "AvailableModels": [
    {
      "Id": "deberta-v3-base-prompt-injection-v2",
      "Name": "DeBERTa v3 Base (Latest)",
      "Description": "Latest version with improved accuracy",
      "Version": "v2",
      "Labels": [ "SAFE", "INJECTION" ],
      "MaxSequenceLength": 512,
      "Architecture": "deberta-v3-base",
      "Status": "available"
    },
    {
      "Id": "deberta-v3-base-prompt-injection-v1",
      "Name": "DeBERTa v3 Base (Legacy)",
      "Description": "Previous version, maintained for compatibility",
      "Version": "v1",
      "Labels": [ "SAFE", "INJECTION" ],
      "MaxSequenceLength": 512,
      "Architecture": "deberta-v3-base",
      "Status": "deprecated"
    },
    {
      "Id": "deberta-v3-large-prompt-injection",
      "Name": "DeBERTa v3 Large",
      "Description": "High-accuracy model for critical applications",
      "Version": "v1",
      "Labels": [ "SAFE", "INJECTION" ],
      "MaxSequenceLength": 512,
      "Architecture": "deberta-v3-large",
      "Status": "available"
    }
  ]
}
```

## Troubleshooting

### Models not appearing in API
- Check JSON syntax in `appsettings.json`
- Ensure the API has been restarted after configuration changes
- Check application logs for configuration errors

### 404 errors when using model
- Verify the model ID matches exactly (case-sensitive)
- Check `/api/models` to see registered models
- Ensure the model is marked as "available"

## Notes

- Model configuration is loaded at application startup
- Changes require an application restart
- The actual ML model files are still configured separately in `ModelConfiguration`
- This configuration only registers which model identifiers are available through the API

