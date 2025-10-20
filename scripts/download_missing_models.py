#!/usr/bin/env python3
"""
Dynamic model downloader that automatically discovers and downloads missing models.
This script:
1. Reads model configuration from models.json
2. Checks if models already exist
3. Downloads only missing models
"""

import os
import sys
import subprocess
from pathlib import Path
import json

def load_model_config(config_file="config/models.json"):
    """Load model configuration from JSON file."""
    # Support both relative to script dir and relative to project root
    script_dir = Path(__file__).parent
    project_root = script_dir.parent
    
    # Try project root first (config/models.json)
    config_path = project_root / config_file
    
    # Fallback to script directory for backward compatibility
    if not config_path.exists() and not config_file.startswith("config/"):
        config_path = script_dir / config_file
    
    if not config_path.exists():
        print(f"[WARN] Configuration file not found: {config_path}")
        print("Using default empty configuration")
        return {"models": [], "config": {}}
    
    try:
        with open(config_path, 'r', encoding='utf-8') as f:
            config = json.load(f)
        print(f"[OK] Loaded configuration from {config_path}")
        print(f"  Found {len(config.get('models', []))} model(s) defined")
        return config
    except json.JSONDecodeError as e:
        print(f"[ERROR] Error parsing JSON configuration: {e}")
        sys.exit(1)
    except Exception as e:
        print(f"[ERROR] Error loading configuration: {e}")
        sys.exit(1)

def check_model_exists(model_path, check_files):
    """Check if a model already exists by verifying the presence of key files."""
    if isinstance(check_files, str):
        check_files = [check_files]
    
    # Check if at least the primary file (first in list) exists
    primary_file = Path(model_path) / check_files[0]
    return primary_file.exists()

def run_conversion_script(script_path, model_config=None):
    """Run a conversion script and return success status."""
    try:
        print(f"\n{'='*60}")
        print(f"Running: {script_path}")
        print('='*60)
        
        # Build command with arguments for generic converter
        cmd = [sys.executable, str(script_path)]
        
        # If model config provided and script is the generic converter, pass config-id
        if model_config and script_path.name == "convert_to_onnx.py":
            model_id = model_config.get("id")
            if model_id:
                cmd.extend(["--config-id", model_id])
                print(f"Using configuration ID: {model_id}")
        
        result = subprocess.run(
            cmd,
            check=True,
            capture_output=False,
            text=True
        )
        return True
    except subprocess.CalledProcessError as e:
        print(f"[ERROR] Error running {script_path}: {e}")
        return False
    except Exception as e:
        print(f"[ERROR] Unexpected error: {e}")
        return False

def discover_and_download_models(force_download=False, models_base_dir="/models"):
    """
    Discover conversion scripts and download missing models.
    
    Args:
        force_download: If True, download all models regardless of existence
        models_base_dir: Base directory where models should be stored/checked
    """
    print("="*60)
    print("Dynamic Model Downloader")
    print("="*60)
    print(f"Base models directory: {models_base_dir}")
    print(f"Force download: {force_download}")
    print()
    
    # Load configuration from JSON
    json_config = load_model_config()
    models_config = json_config.get("models", [])
    global_config = json_config.get("config", {})
    
    if not models_config:
        print("[WARN] No models configured in config/models.json")
        return True
    
    # Get global conversion script (default to convert_to_onnx.py)
    conversion_script_name = global_config.get("conversion_script", "convert_to_onnx.py")
    scripts_dir = Path(__file__).parent
    conversion_script_path = scripts_dir / conversion_script_name
    
    if not conversion_script_path.exists():
        print(f"[ERROR] Conversion script not found: {conversion_script_path}")
        return False
    
    print(f"Using conversion script: {conversion_script_name}")
    
    missing_models = []
    existing_models = []
    
    # Check which models are missing
    for model_config in models_config:
        model_name = model_config.get("name")
        output_dir_name = model_config.get("output_dir")
        check_files = model_config.get("check_files", ["model.onnx"])
        
        if not model_name or not output_dir_name:
            print(f"[WARN] Warning: Incomplete model configuration: {model_config.get('id', 'unknown')}")
            continue
        
        script_path = conversion_script_path
        
        # Use absolute path for checking in container
        if models_base_dir.startswith("/"):
            model_output_dir = Path(models_base_dir) / output_dir_name
        else:
            model_output_dir = Path(models_base_dir) / output_dir_name
        
        if check_model_exists(model_output_dir, check_files) and not force_download:
            print(f"[OK] {model_name}: Already exists at {model_output_dir}")
            existing_models.append(model_name)
        else:
            status = "Missing" if not check_model_exists(model_output_dir, check_files) else "Forcing re-download"
            print(f"[DOWNLOAD] {model_name}: {status}")
            missing_models.append({
                "id": model_config.get("id"),
                "name": model_name,
                "script": script_path,
                "output_dir": model_output_dir,
                "check_files": check_files,
                "original_config": model_config
            })
    
    # Summary
    print()
    print("="*60)
    print("Summary:")
    print(f"  [OK] Existing models: {len(existing_models)}")
    print(f"  --> Models to download: {len(missing_models)}")
    print("="*60)
    
    if not missing_models:
        print("\n[OK] All models are present. No downloads needed!")
        return True
    
    # Download missing models
    print("\nStarting downloads...")
    success_count = 0
    failed_models = []
    
    for model_info in missing_models:
        print(f"\n{'='*60}")
        print(f"Downloading: {model_info['name']}")
        print(f"Script: {model_info['script'].name}")
        print(f"Output: {model_info['output_dir']}")
        print('='*60)
        
        if run_conversion_script(model_info['script'], model_info.get('original_config')):
            # Check multiple possible locations where the model might have been created
            check_files = model_info['check_files']
            primary_check_file = model_info['output_dir'] / check_files[0]
            
            # Also check workspace location
            output_dir_name = model_info['original_config']['output_dir']
            workspace_model_dir = Path("models") / output_dir_name
            workspace_check = workspace_model_dir / check_files[0]
            
            # If model was created in workspace/models, move it to /models
            if workspace_check.exists() and not primary_check_file.exists():
                print(f"Moving model from {workspace_model_dir} to {model_info['output_dir']}")
                import shutil
                try:
                    # Ensure target directory exists
                    model_info['output_dir'].mkdir(parents=True, exist_ok=True)
                    # Copy all files from workspace to target
                    for item in workspace_model_dir.glob("*"):
                        if item.is_file():
                            shutil.copy2(item, model_info['output_dir'] / item.name)
                    print(f"[OK] Model files moved successfully")
                except Exception as e:
                    print(f"[WARN] Warning: Could not move files: {e}")
            
            # Final verification
            if primary_check_file.exists():
                print(f"[OK] {model_info['name']}: Download successful!")
                success_count += 1
            else:
                print(f"[ERROR] {model_info['name']}: Downloaded but model file not found at {primary_check_file}")
                failed_models.append(model_info['name'])
        else:
            print(f"[ERROR] {model_info['name']}: Download failed")
            failed_models.append(model_info['name'])
    
    # Final summary
    print("\n" + "="*60)
    print("DOWNLOAD COMPLETE")
    print("="*60)
    print(f"[OK] Successful: {success_count}/{len(missing_models)}")
    if failed_models:
        print(f"[ERROR] Failed: {len(failed_models)}")
        for model in failed_models:
            print(f"    - {model}")
    print("="*60)
    
    return len(failed_models) == 0

def check_missing_models_only(models_base_dir="/models"):
    """
    Check which models are missing without downloading.
    Returns the count of missing models.
    """
    # Load configuration from JSON
    json_config = load_model_config()
    models_config = json_config.get("models", [])
    
    missing_count = 0
    
    for model_config in models_config:
        output_dir_name = model_config.get("output_dir")
        check_files = model_config.get("check_files", ["model.onnx"])
        
        if not output_dir_name:
            continue
        
        if models_base_dir.startswith("/"):
            model_output_dir = Path(models_base_dir) / output_dir_name
        else:
            model_output_dir = Path(models_base_dir) / output_dir_name
        
        if not check_model_exists(model_output_dir, check_files):
            missing_count += 1
    
    print(f"Models to download: {missing_count}")
    return missing_count

def main():
    import argparse
    
    parser = argparse.ArgumentParser(
        description="Dynamically discover and download missing ML models"
    )
    parser.add_argument(
        "--force",
        action="store_true",
        help="Force download all models even if they exist"
    )
    parser.add_argument(
        "--models-dir",
        default="/models",
        help="Base directory for models (default: /models)"
    )
    parser.add_argument(
        "--check-only",
        action="store_true",
        help="Only check for missing models without downloading"
    )
    
    args = parser.parse_args()
    
    # If check-only mode, just report and exit
    if args.check_only:
        missing_count = check_missing_models_only(args.models_dir)
        sys.exit(0)
    
    # Determine if we should download based on environment or arguments
    force_download = args.force or os.environ.get("DOWNLOAD_MODELS", "").lower() == "true"
    
    success = discover_and_download_models(
        force_download=force_download,
        models_base_dir=args.models_dir
    )
    
    sys.exit(0 if success else 1)

if __name__ == "__main__":
    main()

