"""
Script to download and convert the DeBERTa v3 Prompt Injection model to ONNX format
"""
from optimum.onnxruntime import ORTModelForSequenceClassification
from transformers import AutoTokenizer
import os

model_name = 'protectai/deberta-v3-base-prompt-injection-v2'
output_dir = 'models'

print(f"Downloading and converting {model_name}...")
print("This may take a few minutes on first run...")

# Create output directory if it doesn't exist
os.makedirs(output_dir, exist_ok=True)

# Load and export the model to ONNX
print("\n[1/3] Loading model from HuggingFace...")
model = ORTModelForSequenceClassification.from_pretrained(model_name, export=True)

print("[2/3] Loading tokenizer...")
tokenizer = AutoTokenizer.from_pretrained(model_name)

# Save the model
print(f"[3/3] Saving model files to {output_dir}...")
model.save_pretrained(output_dir)
tokenizer.save_pretrained(output_dir)

print("\n✓ Model conversion complete!")
print(f"\nFiles saved to: {output_dir}")
print("  - model.onnx")
print("  - tokenizer.json")
print("  - config.json")
print("\nYou can now run the API:")
print("  cd src/DebertaPromptGuard.Api")
print("  dotnet run")

