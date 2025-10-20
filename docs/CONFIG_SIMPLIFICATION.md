# Configuration Simplification

## 🎯 What Changed

Removed the redundant `conversion_script` field from individual model configurations. Since all models now use the same universal converter, it's configured globally instead.

## 📊 Before vs After

### Before: Redundant Per-Model Field

```json
{
  "models": [
    {
      "id": "model-1",
      "name": "Model 1",
      "conversion_script": "convert_to_onnx.py",  ← Repeated for every model
      ...
    },
    {
      "id": "model-2",
      "name": "Model 2",
      "conversion_script": "convert_to_onnx.py",  ← Same value repeated
      ...
    }
  ],
  "config": {
    "conversion_script": "convert_to_onnx.py"    ← Also in global config
  }
}
```

**Problems:**
- ❌ Duplicate information (DRY violation)
- ❌ More to type for each model
- ❌ Potential for inconsistency
- ❌ Harder to change converter globally

### After: Single Global Configuration

```json
{
  "models": [
    {
      "id": "model-1",
      "name": "Model 1",
      ...                                         ← No conversion_script field
    },
    {
      "id": "model-2",
      "name": "Model 2",
      ...                                         ← Clean and simple
    }
  ],
  "config": {
    "conversion_script": "convert_to_onnx.py"    ← Defined once, globally
  }
}
```

**Benefits:**
- ✅ DRY (Don't Repeat Yourself)
- ✅ Less configuration per model
- ✅ Single source of truth
- ✅ Easy to change converter globally

## 🔄 How It Works Now

### Configuration Loading

1. **Load global config**
   ```python
   global_config = json_config.get("config", {})
   conversion_script = global_config.get("conversion_script", "convert_to_onnx.py")
   ```

2. **Apply to all models**
   ```python
   for model_config in models_config:
       # Use the global conversion script for this model
       script_path = scripts_dir / conversion_script
   ```

3. **Fallback to default**
   - If not specified in config: uses `convert_to_onnx.py`
   - Backward compatible

### Model Configuration Structure

```json
{
  "models": [
    {
      "id": "my-model",              // Required: Unique identifier
      "name": "My Model",             // Required: Display name
      "huggingface_model": "...",     // Required: HF model ID
      "output_dir": "my-model",       // Required: Output directory
      "check_files": [...],           // Optional: Verification files
      "task_type": "...",             // Optional: Model task type
      "max_length": 512,              // Optional: Sequence length
      "opset_version": 14,            // Optional: ONNX version
      "required": true,               // Optional: Is mandatory?
      "description": "..."            // Optional: Description
    }
  ],
  "config": {
    "conversion_script": "convert_to_onnx.py",  // Global converter
    "models_base_dir": "/models",               // Global models dir
    "python_requirements": [...]                // Global dependencies
  }
}
```

## ✅ Benefits

### 1. Simpler Model Definitions

**Before (9 lines):**
```json
{
  "id": "bert-model",
  "name": "BERT",
  "huggingface_model": "bert-base-uncased",
  "conversion_script": "convert_to_onnx.py",
  "output_dir": "bert",
  "task_type": "sequence-classification",
  "required": true,
  "description": "BERT base model"
}
```

**After (8 lines):**
```json
{
  "id": "bert-model",
  "name": "BERT",
  "huggingface_model": "bert-base-uncased",
  "output_dir": "bert",
  "task_type": "sequence-classification",
  "required": true,
  "description": "BERT base model"
}
```

### 2. Easy Global Changes

Want to use a different converter?

**Before:** Edit every single model definition
```json
// Update 10 models x 1 field = 10 changes
"conversion_script": "new_converter.py"  // Repeated 10 times
```

**After:** Edit once in global config
```json
"config": {
  "conversion_script": "new_converter.py"  // One change
}
```

### 3. Consistency Guaranteed

- ✅ All models use the same converter
- ✅ No risk of typos in individual models
- ✅ Single point of control

### 4. Cleaner JSON

Less visual noise, easier to read:

```json
{
  "models": [
    {"id": "model-1", "name": "Model 1", ...},
    {"id": "model-2", "name": "Model 2", ...},
    {"id": "model-3", "name": "Model 3", ...}
  ],
  "config": {
    "conversion_script": "convert_to_onnx.py"
  }
}
```

## 🔍 Implementation Details

### Code Changes

#### download_missing_models.py

```python
# OLD: Get script from each model
for model_config in models_config:
    script_name = model_config.get("conversion_script")  ← Per model
    script_path = scripts_dir / script_name

# NEW: Get script from global config once
conversion_script_name = global_config.get("conversion_script", "convert_to_onnx.py")
conversion_script_path = scripts_dir / conversion_script_name

for model_config in models_config:
    script_path = conversion_script_path  ← Same for all
```

### Backward Compatibility

The script still works if models have their own `conversion_script` field:

```python
# If model has its own script (legacy), use it
model_script = model_config.get("conversion_script")
if model_script:
    script_path = scripts_dir / model_script
else:
    script_path = conversion_script_path  # Use global
```

**Note:** This backward compatibility is NOT implemented yet, but could be added if needed.

## 📝 Migration Guide

### For Existing Configurations

1. **Remove from models:**
   ```json
   // Remove this line from each model:
   "conversion_script": "convert_to_onnx.py",
   ```

2. **Ensure in global config:**
   ```json
   "config": {
     "conversion_script": "convert_to_onnx.py"
   }
   ```

3. **Test:**
   ```bash
   python scripts/download_missing_models.py --check-only
   ```

### For New Models

Just don't include `conversion_script` field:

```json
{
  "id": "new-model",
  "name": "New Model",
  "huggingface_model": "author/model",
  "output_dir": "new-model"
  // No conversion_script needed!
}
```

## 🎯 Best Practices

### 1. Global by Default

Put shared configuration in `config` section:

```json
"config": {
  "conversion_script": "convert_to_onnx.py",
  "models_base_dir": "/models",
  "default_task_type": "sequence-classification",
  "default_max_length": 512
}
```

### 2. Override Only When Needed

If a specific model needs different settings:

```json
{
  "id": "special-model",
  "task_type": "token-classification",  // Override global default
  "max_length": 1024                    // Override global default
}
```

### 3. Document Global Settings

Add comments in JSON (if your parser supports it) or in README:

```json
"config": {
  "conversion_script": "convert_to_onnx.py",  // Universal HF converter
  "models_base_dir": "/models"                // Container path
}
```

## 📊 Impact

### Lines of Code Reduced

For a config with 10 models:
- **Before:** 1 global + 10 per-model = 11 occurrences
- **After:** 1 global only = 1 occurrence
- **Reduction:** 91% less redundancy

### Configuration Size

Example with 5 models:
- **Before:** ~250 lines
- **After:** ~245 lines
- **Savings:** ~5 lines (2% smaller, cleaner)

## ✨ Summary

**The `conversion_script` field is now:**
- ❌ **Removed** from individual model definitions
- ✅ **Defined once** in global `config` section
- ✅ **Applied** to all models automatically
- ✅ **Cleaner** configuration with less repetition

**To add a new model, you no longer need to specify the conversion script - it's automatic!** 🚀

### Quick Reference

**Minimal model definition:**
```json
{
  "id": "my-model",
  "name": "My Model",
  "huggingface_model": "author/model-name",
  "output_dir": "my-model"
}
```

**That's it!** Everything else is either optional or globally configured.


