# Unified Model Converter

## 🎯 What Changed

We've consolidated all model-specific conversion scripts into **one universal converter** that handles any HuggingFace model.

## 📊 Before vs After

### Before: Multiple Custom Scripts

```
scripts/
├── convert_model.py                       ← Custom for DeBERTa
├── convert_solidity_vulnerability.py      ← Custom for GraphCodeBERT
└── download_missing_models.py
```

**Problems:**
- ❌ New script needed for each model
- ❌ Duplicated conversion logic
- ❌ Harder to maintain consistency
- ❌ More code to test

### After: One Generic Script

```
scripts/
├── convert_to_onnx.py                     ← Universal converter
├── download_missing_models.py
└── models.json                            ← Configuration
```

**Benefits:**
- ✅ One script handles all models
- ✅ Configuration-driven
- ✅ Consistent conversion logic
- ✅ Less code to maintain
- ✅ Easier to add new models

## 🔄 How It Works

### Architecture

```
┌─────────────────────────────────────────────────────────┐
│  models.json                                            │
│  ┌───────────────────────────────────────────────────┐ │
│  │ {                                                 │ │
│  │   "id": "my-model",                               │ │
│  │   "huggingface_model": "bert-base-uncased",       │ │
│  │   "conversion_script": "convert_to_onnx.py"       │ │
│  │ }                                                 │ │
│  └───────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────┐
│  download_missing_models.py                             │
│  • Reads models.json                                    │
│  • Checks what's missing                                │
│  • Calls convert_to_onnx.py with --config-id           │
└─────────────────────────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────┐
│  convert_to_onnx.py                                     │
│  • Receives --config-id parameter                       │
│  • Loads config from models.json                        │
│  • Downloads from HuggingFace                           │
│  • Converts to ONNX                                     │
│  • Saves to output directory                            │
└─────────────────────────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────┐
│  models/my-model/                                       │
│  ├── model.onnx                                         │
│  ├── tokenizer.json                                     │
│  └── config.json                                        │
└─────────────────────────────────────────────────────────┘
```

## 📝 New models.json Format

### Complete Example

```json
{
  "models": [
    {
      "id": "deberta-prompt-injection",
      "name": "DeBERTa Prompt Injection",
      "huggingface_model": "protectai/deberta-v3-base-prompt-injection-v2",
      "conversion_script": "convert_to_onnx.py",
      "output_dir": "deberta-v3-base-prompt-injection-v2",
      "check_files": ["model.onnx", "tokenizer.json"],
      "task_type": "sequence-classification",
      "max_length": 512,
      "opset_version": 14,
      "required": true,
      "description": "DeBERTa v3 model for detecting prompt injection"
    }
  ],
  "config": {
    "models_base_dir": "/models",
    "conversion_script": "convert_to_onnx.py"
  }
}
```

### New Fields

| Field | Description | Example |
|-------|-------------|---------|
| `huggingface_model` | **NEW!** HuggingFace model ID | `"bert-base-uncased"` |
| `task_type` | **NEW!** Model task type | `"sequence-classification"` |
| `max_length` | **NEW!** Max sequence length | `512` |
| `opset_version` | **NEW!** ONNX opset version | `14` |

## 🚀 Adding a New Model

### Super Simple Process

**OLD WAY (3 steps):**
1. Write Python conversion script
2. Add to models.json
3. Test custom script

**NEW WAY (1 step):**
1. Add to models.json 🎉

### Example: Adding RoBERTa

```json
{
  "id": "roberta-sentiment",
  "name": "RoBERTa Sentiment Analysis",
  "huggingface_model": "cardiffnlp/twitter-roberta-base-sentiment",
  "conversion_script": "convert_to_onnx.py",
  "output_dir": "roberta-sentiment",
  "check_files": ["model.onnx", "tokenizer.json"],
  "task_type": "sequence-classification",
  "description": "RoBERTa model for sentiment analysis"
}
```

**Done!** No Python code required.

## 🔧 convert_to_onnx.py Features

### 1. Config-Driven Mode

```bash
# Load everything from models.json
python convert_to_onnx.py --config-id deberta-prompt-injection
```

The script will:
- ✅ Load config from models.json
- ✅ Download from HuggingFace
- ✅ Convert to ONNX
- ✅ Save to correct location

### 2. Command-Line Mode

```bash
# Manual override for testing
python convert_to_onnx.py \
  --model-id "bert-base-uncased" \
  --output-dir "./models/bert" \
  --model-name "BERT Base" \
  --max-length 512
```

### 3. Universal Converter

Works with any HuggingFace model:
- ✅ BERT family (BERT, RoBERTa, DeBERTa)
- ✅ GraphCodeBERT
- ✅ CodeBERT
- ✅ DistilBERT
- ✅ Any sequence classification model
- ✅ Future: token classification, QA, etc.

## 📋 Migration Guide

### Step 1: Update models.json

Add `huggingface_model` field to each model:

```json
{
  "id": "existing-model",
  "name": "Existing Model",
  "huggingface_model": "author/model-on-huggingface",  ← ADD THIS
  "conversion_script": "convert_to_onnx.py",           ← CHANGE THIS
  "output_dir": "existing-model",
  ...
}
```

### Step 2: Test

```bash
# Check configuration
python scripts/download_missing_models.py --check-only

# Test download (dry run)
python scripts/convert_to_onnx.py --config-id your-model-id --output-dir ./test
```

### Step 3: Deploy

```bash
# Build Docker image
docker build -t andy-inference-models .
```

### Step 4: Cleanup (Optional)

```bash
# Remove old scripts
rm scripts/convert_model.py
rm scripts/convert_solidity_vulnerability.py
```

## ✅ Benefits Summary

| Aspect | Before | After |
|--------|--------|-------|
| **Scripts per model** | 1 custom script | 0 (shared script) |
| **Lines of code** | ~100 per model | ~0 per model |
| **Add new model** | Write Python | Edit JSON |
| **Conversion logic** | Duplicated | Centralized |
| **Maintenance** | N scripts to update | 1 script to update |
| **Testing** | Test each script | Test once |
| **Consistency** | May vary | Always consistent |

## 🎯 Key Advantages

### 1. Simplified Maintenance

- ✅ Fix bugs in one place
- ✅ Add features once, benefit all models
- ✅ Consistent error handling

### 2. Easy Extensibility

Want to add retries? Rate limiting? Caching?
- **Before:** Update every conversion script
- **After:** Update `convert_to_onnx.py` once

### 3. Better Documentation

All model parameters are now visible in `models.json`:
```json
{
  "max_length": 512,         ← Clearly documented
  "task_type": "sequence-classification",  ← Visible configuration
  "opset_version": 14        ← Easy to change
}
```

### 4. Future-Proof

Easy to add support for:
- Different model sources (not just HuggingFace)
- Different export formats (TensorRT, TFLite)
- Model quantization
- Model optimization flags

## 🧪 Testing

```bash
# Test configuration loading
python scripts/convert_to_onnx.py --config-id deberta-prompt-injection --output-dir ./test-output

# Test manual mode
python scripts/convert_to_onnx.py \
  --model-id "bert-base-uncased" \
  --output-dir "./test-bert"

# Test full pipeline
python scripts/download_missing_models.py --models-dir ./test-models
```

## 🎉 Summary

**We went from:**
- Multiple custom scripts (100+ lines each)
- Duplicated logic
- Manual per-model code

**To:**
- One universal script (~200 lines total)
- JSON configuration
- Zero code per model

**Adding a model is now:**
```json
// Just add this to models.json!
{
  "id": "my-new-model",
  "name": "My New Model",
  "huggingface_model": "author/model-name",
  "conversion_script": "convert_to_onnx.py",
  "output_dir": "my-new-model"
}
```

**That's it!** 🚀


