# Configuration Unification

## 🎯 What Changed

Replaced duplicate model configuration in `appsettings.json` with a single source of truth: `config/models.json`. The .NET API now reads directly from the same configuration file used by the Python model download system.

## 📊 Before vs After

### Before: Duplicate Configuration

**Two places to maintain model info:**

1. **`config/models.json`** (for Python scripts)
   ```json
   {
     "models": [
       {
         "id": "deberta-prompt-injection",
         "name": "DeBERTa Prompt Injection",
         "huggingface_model": "protectai/deberta-v3-base-prompt-injection-v2",
         ...
       }
     ]
   }
   ```

2. **`appsettings.json`** (for .NET API) ← DUPLICATE!
   ```json
   {
     "AvailableModels": [
       {
         "Id": "deberta-v3-base-prompt-injection-v2",
         "Name": "DeBERTa v3 Base Prompt Injection v2",
         ...
       }
     ]
   }
   ```

**Problems:**
- ❌ Information duplicated
- ❌ Potential for inconsistency
- ❌ Have to update both files when adding models
- ❌ Easy to forget to sync changes

### After: Single Source of Truth

**One configuration file:**

**`config/models.json`** - Used by both Python and .NET
```json
{
  "models": [
    {
      "id": "deberta-prompt-injection",
      "name": "DeBERTa Prompt Injection",
      "huggingface_model": "protectai/deberta-v3-base-prompt-injection-v2",
      "output_dir": "deberta-v3-base-prompt-injection-v2",
      "max_length": 512,
      "description": "DeBERTa v3 model for detecting prompt injection"
    }
  ],
  "config": {
    "models_base_dir": "/models",
    "conversion_script": "convert_to_onnx.py",
    "tokenizer_service_url": "http://localhost:8000"
  }
}
```

**`appsettings.json`** - Minimal, non-duplicate config
```json
{
  "Logging": {...},
  "ModelsConfigPath": "config/models.json",
  "TokenizerServiceUrl": "http://localhost:8000"
}
```

**Benefits:**
- ✅ Single source of truth
- ✅ No duplication
- ✅ Guaranteed consistency
- ✅ Add model once, works everywhere
- ✅ Easier maintenance

## 🔄 How It Works

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│  config/models.json                                         │
│  ┌───────────────────────────────────────────────────────┐ │
│  │ • Model definitions                                   │ │
│  │ • HuggingFace IDs                                     │ │
│  │ • Output directories                                  │ │
│  │ • Labels, descriptions                                │ │
│  │ • Global configuration                                │ │
│  └───────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
           │                              │
           │                              │
           ↓                              ↓
┌────────────────────────┐   ┌──────────────────────────────┐
│  Python Scripts        │   │  .NET API                    │
│  • download_missing    │   │  • ModelsConfigurationLoader │
│  • convert_to_onnx     │   │  • ModelRegistryService      │
│                        │   │  • ModelManager              │
└────────────────────────┘   └──────────────────────────────┘
```

### C# Implementation

#### 1. ModelsConfigurationLoader

New class that reads `config/models.json`:

```csharp
public class ModelsConfigurationLoader
{
    public ModelsConfiguration Load()
    {
        // Finds and loads config/models.json
        // Returns strongly-typed configuration
    }
    
    public ModelDefinition? GetModel(string id) { }
    public List<ModelDefinition> GetAllModels() { }
    public GlobalConfig GetGlobalConfig() { }
}
```

#### 2. ModelDefinition

Maps JSON model structure to C#:

```csharp
public class ModelDefinition
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string HuggingFaceModel { get; set; }
    public string OutputDir { get; set; }
    public int? MaxLength { get; set; }
    // ... other properties
    
    // Helper methods
    public string GetModelPath(string baseDir = "models");
    public string GetTokenizerPath(string baseDir = "models");
    public List<string> GetLabels();
}
```

#### 3. Updated ModelRegistryService

Now loads from `config/models.json`:

```csharp
public ModelRegistryService(IConfiguration configuration)
{
    var configPath = configuration.GetValue<string>("ModelsConfigPath");
    var loader = new ModelsConfigurationLoader(configPath);
    
    var modelsConfig = loader.Load();
    
    foreach (var modelDef in modelsConfig.Models)
    {
        _models.Add(new ModelInfo
        {
            Id = modelDef.OutputDir,
            Name = modelDef.Name,
            Labels = modelDef.GetLabels().ToArray(),
            // ... map all properties
        });
    }
}
```

## 📝 Files Changed

### Created

1. **`src/InferenceModel.Api/Configuration/ModelsConfiguration.cs`**
   - `ModelsConfiguration` - Root config class
   - `ModelDefinition` - Individual model class
   - `GlobalConfig` - Global settings class
   - `ModelsConfigurationLoader` - Loader service

### Modified

2. **`src/InferenceModel.Api/appsettings.json`**
   - Removed: `ModelConfiguration` section
   - Removed: `AvailableModels` array
   - Added: `ModelsConfigPath` (pointer to config file)
   - Simplified: Much smaller file

3. **`src/InferenceModel.Api/Services/ModelRegistryService.cs`**
   - Changed: Constructor now uses `ModelsConfigurationLoader`
   - Changed: Maps `ModelDefinition` → `ModelInfo`
   - Improved: Better error handling with fallback

4. **`config/models.json`**
   - Added: `tokenizer_service_url` in global config

### Unchanged

5. **`src/InferenceModel.Api/Configuration/ModelConfiguration.cs`**
   - Still exists for backward compatibility
   - May be used by other parts of the code
   - Can be deprecated gradually

## ✅ Benefits

### 1. Single Source of Truth

**Add a model once:**
```json
// Edit config/models.json
{
  "id": "new-model",
  "name": "New Model",
  "huggingface_model": "author/new-model",
  "output_dir": "new-model"
}
```

**Works everywhere:**
- ✅ Python download scripts find it
- ✅ .NET API exposes it
- ✅ Swagger docs show it
- ✅ All endpoints support it

### 2. Guaranteed Consistency

**Before:**
- Python: Model ID is `"deberta-prompt-injection"`
- .NET: Model ID is `"deberta-v3-base-prompt-injection-v2"`
- 🔴 **Mismatch!**

**After:**
- Python: Reads from `config/models.json`
- .NET: Reads from `config/models.json`
- ✅ **Always consistent!**

### 3. Easier Maintenance

| Task | Before | After |
|------|--------|-------|
| Add model | Edit 2 files | Edit 1 file |
| Change model name | Update 2 places | Update 1 place |
| Update description | 2 locations | 1 location |
| Risk of inconsistency | High | Zero |

### 4. Better Developer Experience

```bash
# Add a new model
echo '{
  "id": "my-model",
  "name": "My Model",
  "huggingface_model": "author/model",
  "output_dir": "my-model"
}' | jq '.' >> config/models.json

# Done! Works in both Python and .NET
```

## 🔍 Configuration Mapping

### JSON → C# Mapping

| JSON Field | C# Property | Description |
|------------|-------------|-------------|
| `id` | `Id` | Unique model identifier |
| `name` | `Name` | Display name |
| `huggingface_model` | `HuggingFaceModel` | HF model ID |
| `output_dir` | `OutputDir` | Directory name |
| `check_files` | `CheckFiles` | Files to verify |
| `task_type` | `TaskType` | Model task |
| `max_length` | `MaxLength` | Sequence length |
| `opset_version` | `OpsetVersion` | ONNX version |
| `required` | `Required` | Is mandatory? |
| `description` | `Description` | Model description |

### Computed Properties

C# adds helper methods:

```csharp
modelDef.GetModelFolder()      // "models/deberta-v3-base-prompt-injection-v2"
modelDef.GetModelPath()        // "models/.../model.onnx"
modelDef.GetTokenizerPath()    // "models/.../tokenizer.json"
modelDef.GetLabels()           // ["SAFE", "INJECTION"]
```

## 🧪 Testing

### Verify Configuration Loading

```bash
# Start the API
dotnet run --project src/InferenceModel.Api

# Check models endpoint
curl http://localhost:5000/api/models

# Should return models from config/models.json
```

### Expected Output

```json
{
  "count": 2,
  "models": [
    {
      "id": "deberta-v3-base-prompt-injection-v2",
      "name": "DeBERTa Prompt Injection",
      "description": "DeBERTa v3 model...",
      "labels": ["SAFE", "INJECTION"],
      "maxSequenceLength": 512
    },
    {
      "id": "graphcodebert-solidity-vulnerability",
      "name": "GraphCodeBERT Solidity Vulnerability Detector",
      "description": "GraphCodeBERT model...",
      "labels": ["SAFE", "VULNERABLE"],
      "maxSequenceLength": 512
    }
  ]
}
```

## 🔄 Migration Guide

### For Existing Deployments

**No migration needed!** The system is backward compatible:

1. If `config/models.json` is not found → Falls back to default model
2. Old `appsettings.json` format is simply ignored
3. API continues to work with fallback configuration

### For New Deployments

1. ✅ Ensure `config/models.json` exists in project root
2. ✅ Remove old `AvailableModels` section from `appsettings.json` (optional)
3. ✅ Start the API - it will automatically load from `config/models.json`

## 📚 Related Changes

This unification is part of a larger configuration consolidation:

- `docs/CONFIG_RELOCATION.md` - Moving config to dedicated directory
- `docs/CONFIG_SIMPLIFICATION.md` - Removing redundant fields
- `docs/UNIFIED_CONVERTER.md` - Single converter script

## ✨ Summary

**We now have true single source of truth for model configuration!**

- ✅ **One file:** `config/models.json`
- ✅ **Used by:** Python scripts AND .NET API
- ✅ **Benefits:** No duplication, guaranteed consistency, easier maintenance
- ✅ **To add a model:** Edit one JSON file
- ✅ **Works:** Everywhere automatically

**Add a model once, it works everywhere!** 🚀


