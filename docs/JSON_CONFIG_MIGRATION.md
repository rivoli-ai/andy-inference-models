# JSON Configuration Migration

## 🎯 What Changed

We've migrated from hardcoded Python dictionaries to a **JSON-based configuration system** for managing ML models.

## 📊 Before vs After

### Before: Hardcoded in Python
```python
# download_missing_models.py
MODEL_CONFIGS = {
    "convert_model.py": {
        "name": "DeBERTa Prompt Injection",
        "output_dir": "models/deberta-v3-base-prompt-injection-v2",
        "check_file": "model.onnx"
    },
    "convert_solidity_vulnerability.py": {
        "name": "GraphCodeBERT Solidity Vulnerability",
        "output_dir": "models/graphcodebert-solidity-vulnerability",
        "check_file": "model.onnx"
    }
}
```

**Problems:**
- ❌ Need to edit Python code to add models
- ❌ Mixed configuration with logic
- ❌ Harder to validate and review
- ❌ Can't be easily edited by non-Python developers

### After: JSON Configuration
```json
{
  "models": [
    {
      "id": "deberta-prompt-injection",
      "name": "DeBERTa Prompt Injection",
      "conversion_script": "convert_model.py",
      "output_dir": "deberta-v3-base-prompt-injection-v2",
      "check_files": ["model.onnx", "tokenizer.json"],
      "required": true,
      "description": "DeBERTa v3 model for prompt injection detection"
    },
    {
      "id": "graphcodebert-vulnerability",
      "name": "GraphCodeBERT Solidity Vulnerability",
      "conversion_script": "convert_solidity_vulnerability.py",
      "output_dir": "graphcodebert-solidity-vulnerability",
      "check_files": ["model.onnx", "tokenizer.json", "vocab.json"],
      "required": true,
      "description": "GraphCodeBERT for Solidity vulnerability detection"
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

**Benefits:**
- ✅ Edit JSON to add models (no Python needed!)
- ✅ Clear separation of config and code
- ✅ Easy to validate with JSON schema
- ✅ Can be edited by anyone
- ✅ More detailed metadata (id, description, required)
- ✅ Support for multiple check files
- ✅ Future-proof (can add more fields)

## 🔄 Migration Path

### Adding a New Model

**Before (Old Way):**
1. Create `scripts/convert_new_model.py`
2. Edit `download_missing_models.py`
3. Add entry to `MODEL_CONFIGS` dictionary
4. Total: **2 files to edit**, Python knowledge required

**After (New Way):**
1. Create `scripts/convert_new_model.py`
2. Edit `scripts/models.json`
3. Add JSON entry
4. Total: **2 files to edit**, but only JSON knowledge required! ✅

### Example: Adding a New Model

```json
{
  "models": [
    // ... existing models ...
    {
      "id": "code-security-scanner",
      "name": "Code Security Scanner",
      "conversion_script": "convert_security_scanner.py",
      "output_dir": "code-security-scanner",
      "check_files": ["model.onnx", "tokenizer.json"],
      "required": false,
      "description": "Scans code for security vulnerabilities"
    }
  ]
}
```

Done! No Python code changes needed.

## 📋 New Features

### 1. Multiple Check Files
Instead of single `check_file`, now support array:
```json
"check_files": ["model.onnx", "tokenizer.json", "vocab.json"]
```

### 2. Model Metadata
```json
{
  "id": "unique-identifier",
  "required": true,
  "description": "Detailed description"
}
```

### 3. Global Configuration
```json
"config": {
  "models_base_dir": "/models",
  "python_requirements": [...]
}
```

### 4. Better Validation
- Script checks for missing required fields
- Clear error messages for invalid configuration
- Validates JSON structure on load

## 🔍 Implementation Details

### Key Changes to `download_missing_models.py`

1. **Added `load_model_config()` function**
   ```python
   def load_model_config(config_file="models.json"):
       config_path = Path(__file__).parent / config_file
       with open(config_path, 'r') as f:
           return json.load(f)
   ```

2. **Updated `discover_and_download_models()`**
   - Now reads from JSON instead of hardcoded dict
   - Supports multiple check files
   - Better error handling

3. **Updated `check_missing_models_only()`**
   - Uses JSON configuration
   - Consistent with download logic

4. **Updated `check_model_exists()`**
   - Accepts array of check files
   - More flexible verification

## 🎨 JSON Schema (for validation)

You can validate `models.json` with this schema:

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "required": ["models"],
  "properties": {
    "models": {
      "type": "array",
      "items": {
        "type": "object",
        "required": ["id", "name", "conversion_script", "output_dir"],
        "properties": {
          "id": { "type": "string", "pattern": "^[a-z0-9-]+$" },
          "name": { "type": "string" },
          "conversion_script": { "type": "string", "pattern": "\\.py$" },
          "output_dir": { "type": "string" },
          "check_files": {
            "type": "array",
            "items": { "type": "string" },
            "minItems": 1
          },
          "required": { "type": "boolean" },
          "description": { "type": "string" }
        }
      }
    },
    "config": {
      "type": "object",
      "properties": {
        "models_base_dir": { "type": "string" },
        "python_requirements": {
          "type": "array",
          "items": { "type": "string" }
        }
      }
    }
  }
}
```

## 🚀 Benefits Summary

| Aspect | Before | After |
|--------|--------|-------|
| **Add Model** | Edit Python code | Edit JSON file |
| **Skill Level** | Python developer | Anyone who can edit JSON |
| **Configuration** | Mixed with code | Separate file |
| **Validation** | Manual | Can use JSON schema |
| **Metadata** | Limited | Rich (id, description, required) |
| **Check Files** | Single file | Multiple files |
| **Review** | Code review | Simple config review |
| **CI/CD** | Code changes | Config changes |

## 📝 Future Enhancements

With JSON configuration, we can easily add:

1. **HuggingFace model URLs directly in JSON**
   ```json
   "huggingface_model": "protectai/deberta-v3-base-prompt-injection-v2"
   ```

2. **Model versions**
   ```json
   "version": "1.0.0",
   "min_version": "1.0.0"
   ```

3. **Dependencies between models**
   ```json
   "depends_on": ["base-tokenizer"]
   ```

4. **Download sources**
   ```json
   "source": "huggingface",
   "fallback_url": "https://..."
   ```

5. **Conditional downloads**
   ```json
   "download_if": "feature.code_analysis.enabled"
   ```

## ✅ Testing

Test the new configuration:

```bash
# Validate JSON syntax
python -m json.tool scripts/models.json

# Test model discovery
python scripts/download_missing_models.py --check-only

# Test download
python scripts/download_missing_models.py --models-dir ./test-models
```

## 🎉 Summary

**The system is now fully configuration-driven!**

- ✅ No hardcoded model lists
- ✅ Easy to add/modify models
- ✅ Clear separation of concerns
- ✅ Better documentation in config
- ✅ Future-proof and extensible

Just edit `scripts/models.json` to manage your models! 🚀


