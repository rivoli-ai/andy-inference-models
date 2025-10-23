#!/usr/bin/env python3
"""
Generic ONNX model converter for HuggingFace models.
This script can convert any HuggingFace model to ONNX format.
Model configuration is read from models.json or passed as command-line arguments.
"""

import os
import sys
import json
import argparse
from pathlib import Path
from transformers import AutoTokenizer, AutoModelForSequenceClassification
import torch


def convert_model_to_onnx(
    model_id,
    output_dir,
    model_name=None,
    task_type="sequence-classification",
    max_length=512,
    opset_version=14
):
    """
    Convert a HuggingFace model to ONNX format.
    
    Args:
        model_id: HuggingFace model ID (e.g., "protectai/deberta-v3-base-prompt-injection-v2")
        output_dir: Output directory for the converted model
        model_name: Human-readable model name for logging
        task_type: Model task type (sequence-classification, token-classification, etc.)
        max_length: Maximum sequence length for the model
        opset_version: ONNX opset version
    """
    output_path = Path(output_dir)
    display_name = model_name or model_id
    
    print("="*70)
    print(f"ONNX Model Converter")
    print("="*70)
    print(f"Model: {display_name}")
    print(f"HuggingFace ID: {model_id}")
    print(f"Output Directory: {output_path.absolute()}")
    print(f"Task Type: {task_type}")
    print("="*70)
    
    # Create output directory
    output_path.mkdir(parents=True, exist_ok=True)
    
    try:
        # Step 1: Load tokenizer
        print(f"\n[1/4] Loading tokenizer from HuggingFace...")
        tokenizer = AutoTokenizer.from_pretrained(model_id)
        print(f"[OK] Tokenizer loaded successfully")
        
        # Step 2: Save tokenizer
        print(f"\n[2/4] Saving tokenizer files...")
        tokenizer.save_pretrained(output_path)
        
        # List saved tokenizer files
        tokenizer_files = list(output_path.glob("tokenizer*")) + \
                         list(output_path.glob("vocab*")) + \
                         list(output_path.glob("merges*")) + \
                         list(output_path.glob("special_tokens*")) + \
                         list(output_path.glob("*.model"))
        
        print(f"[OK] Tokenizer saved. Files created:")
        for f in sorted(set(tokenizer_files)):
            if f.is_file():
                size_mb = f.stat().st_size / (1024 * 1024)
                print(f"    - {f.name:<35} ({size_mb:>8.2f} MB)")
        
        # Step 3: Load model
        print(f"\n[3/4] Loading model from HuggingFace...")
        print(f"    This may take a few minutes depending on model size...")
        
        if task_type == "sequence-classification":
            model = AutoModelForSequenceClassification.from_pretrained(model_id)
        else:
            # Future: support other task types
            print(f"[WARN] Unsupported task type: {task_type}, using sequence-classification")
            model = AutoModelForSequenceClassification.from_pretrained(model_id)
        
        model.eval()
        
        print(f"[OK] Model loaded successfully")
        if hasattr(model.config, 'num_labels'):
            print(f"    - Number of labels: {model.config.num_labels}")
        if hasattr(model.config, 'id2label'):
            print(f"    - Label mapping: {model.config.id2label}")
        
        # Step 4: Export to ONNX
        print(f"\n[4/4] Converting to ONNX format...")
        print(f"    ONNX Opset Version: {opset_version}")
        print(f"    Maximum Sequence Length: {max_length}")
        print(f"    This may take several minutes...")
        
        # Create dummy input for export
        dummy_text = "This is a sample text for ONNX export."
        dummy_input = tokenizer(
            dummy_text,
            return_tensors="pt",
            max_length=max_length,
            padding="max_length",
            truncation=True
        )
        
        # Export to ONNX
        onnx_path = output_path / "model.onnx"
        
        with torch.no_grad():
            torch.onnx.export(
                model,
                (dummy_input['input_ids'], dummy_input['attention_mask']),
                onnx_path,
                input_names=['input_ids', 'attention_mask'],
                output_names=['logits'],
                dynamic_axes={
                    'input_ids': {0: 'batch_size', 1: 'sequence_length'},
                    'attention_mask': {0: 'batch_size', 1: 'sequence_length'},
                    'logits': {0: 'batch_size'}
                },
                opset_version=opset_version,
                do_constant_folding=True,
                export_params=True
            )
        
        print(f"[OK] ONNX model created: {onnx_path.name}")
        
        # Save config
        model.config.save_pretrained(output_path)
        print(f"[OK] Model configuration saved")
        
        # Final summary
        print("\n" + "="*70)
        print("[OK] CONVERSION COMPLETE!")
        print("="*70)
        print(f"\nModel files saved to: {output_path.absolute()}")
        print(f"\nGenerated files:")
        
        total_size = 0
        for file_path in sorted(output_path.glob("*")):
            if file_path.is_file():
                size = file_path.stat().st_size
                size_mb = size / (1024 * 1024)
                total_size += size
                print(f"  - {file_path.name:<35} ({size_mb:>8.2f} MB)")
        
        total_mb = total_size / (1024 * 1024)
        print(f"\nTotal size: {total_mb:.2f} MB")
        print("\n[OK] Model is ready for inference!")
        print("="*70)
        
        return True
        
    except Exception as e:
        print(f"\n[ERROR] Conversion failed: {e}")
        import traceback
        traceback.print_exc()
        return False


def load_model_config(model_id_to_find):
    """Load model configuration from config/models.json."""
    # Support both relative to script dir and relative to project root
    script_dir = Path(__file__).parent
    project_root = script_dir.parent
    
    # Try project root first (config/models.json)
    config_path = project_root / "config" / "models.json"
    
    # Fallback to script directory for backward compatibility
    if not config_path.exists():
        config_path = script_dir / "models.json"
    
    if not config_path.exists():
        return None
    
    try:
        with open(config_path, 'r', encoding='utf-8') as f:
            config = json.load(f)
        
        # Find the model by ID or output_dir
        for model in config.get("models", []):
            if (model.get("id") == model_id_to_find or 
                model.get("output_dir") == model_id_to_find or
                model.get("huggingface_model") == model_id_to_find):
                return model
        
        return None
    except Exception as e:
        print(f"[WARN] Could not load models.json: {e}")
        return None


def main():
    parser = argparse.ArgumentParser(
        description="Convert HuggingFace models to ONNX format"
    )
    parser.add_argument(
        "--model-id",
        help="HuggingFace model ID or model identifier from models.json"
    )
    parser.add_argument(
        "--output-dir",
        help="Output directory for converted model"
    )
    parser.add_argument(
        "--model-name",
        help="Human-readable model name (for logging)"
    )
    parser.add_argument(
        "--task-type",
        default="sequence-classification",
        help="Model task type (default: sequence-classification)"
    )
    parser.add_argument(
        "--max-length",
        type=int,
        default=512,
        help="Maximum sequence length (default: 512)"
    )
    parser.add_argument(
        "--opset-version",
        type=int,
        default=14,
        help="ONNX opset version (default: 14)"
    )
    parser.add_argument(
        "--config-id",
        help="Load configuration from models.json by ID"
    )
    
    args = parser.parse_args()
    
    # Try to load from config if --config-id is provided or --model-id matches a config
    model_config = None
    if args.config_id:
        model_config = load_model_config(args.config_id)
    elif args.model_id:
        model_config = load_model_config(args.model_id)
    
    # Use config values if available, otherwise use command-line args
    if model_config:
        print(f"[OK] Loaded configuration from models.json")
        model_id = model_config.get("huggingface_model") or args.model_id
        output_dir = args.output_dir or f"models/{model_config.get('output_dir')}"
        model_name = model_config.get("name")
        task_type = model_config.get("task_type", args.task_type)
        max_length = model_config.get("max_length", args.max_length)
        opset_version = model_config.get("opset_version", args.opset_version)
    else:
        # Use command-line arguments
        model_id = args.model_id
        output_dir = args.output_dir
        model_name = args.model_name
        task_type = args.task_type
        max_length = args.max_length
        opset_version = args.opset_version
    
    # Validate required arguments
    if not model_id:
        print("[ERROR] --model-id or --config-id is required")
        parser.print_help()
        sys.exit(1)
    
    if not output_dir:
        print("[ERROR] --output-dir is required (or use --config-id to load from models.json)")
        parser.print_help()
        sys.exit(1)
    
    # Convert the model
    success = convert_model_to_onnx(
        model_id=model_id,
        output_dir=output_dir,
        model_name=model_name,
        task_type=task_type,
        max_length=max_length,
        opset_version=opset_version
    )
    
    sys.exit(0 if success else 1)


if __name__ == "__main__":
    main()

