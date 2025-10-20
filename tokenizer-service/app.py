"""
Tokenizer Microservice
FastAPI service that provides accurate HuggingFace tokenization for AI models
"""
from fastapi import FastAPI, HTTPException, Query
from pydantic import BaseModel
from transformers import AutoTokenizer
import uvicorn
import logging
from typing import List, Dict, Optional
from contextlib import asynccontextmanager

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

# Request/Response models
class TokenizeRequest(BaseModel):
    text: str
    max_length: int = 512
    model: str = "deberta-v3-base-prompt-injection-v2"
    
class TokenizeResponse(BaseModel):
    input_ids: List[int]
    attention_mask: List[int]
    token_count: int
    model: str

class HealthResponse(BaseModel):
    status: str
    available_models: List[str]
    version: str

# Supported models configuration
# Maps model_id to (huggingface_path, local_path)
SUPPORTED_MODELS = {
    "deberta-v3-base-prompt-injection-v2": {
        "hf_path": "protectai/deberta-v3-base-prompt-injection-v2",
        "local_path": "/app/models/deberta-v3-base-prompt-injection-v2"
    },
    "graphcodebert-solidity-vulnerability": {
        "hf_path": "angusleung100/GraphCodeBERT-Base-Solidity-Vulnerability",
        "local_path": "/app/models/graphcodebert-solidity-vulnerability"
    }
}

# Global tokenizers (loaded once at startup)
tokenizers: Dict[str, AutoTokenizer] = {}

def get_tokenizer_path(model_id: str) -> str:
    """
    Determine whether to load tokenizer from local path or HuggingFace.
    Returns the path to use for loading.
    """
    import os
    
    model_config = SUPPORTED_MODELS[model_id]
    local_path = model_config["local_path"]
    hf_path = model_config["hf_path"]
    
    # Check if local tokenizer files exist
    if os.path.exists(local_path):
        # Check for essential tokenizer files
        tokenizer_json = os.path.join(local_path, "tokenizer.json")
        vocab_json = os.path.join(local_path, "vocab.json")
        tokenizer_config = os.path.join(local_path, "tokenizer_config.json")
        
        # If we have tokenizer.json or vocab.json, use local path
        if os.path.exists(tokenizer_json) or os.path.exists(vocab_json):
            logger.info(f"  Found local tokenizer files at {local_path}")
            return local_path
    
    logger.info(f"  Local tokenizer not found, using HuggingFace: {hf_path}")
    return hf_path

@asynccontextmanager
async def lifespan(app: FastAPI):
    """Lifespan event handler for startup and shutdown"""
    # Startup: Load all tokenizers
    global tokenizers
    logger.info("Starting tokenizer service...")
    
    for model_id in SUPPORTED_MODELS.keys():
        logger.info(f"Loading {model_id} tokenizer...")
        tokenizer_path = get_tokenizer_path(model_id)
        
        try:
            tokenizers[model_id] = AutoTokenizer.from_pretrained(tokenizer_path)
            logger.info(f"✓ {model_id} tokenizer loaded successfully from {tokenizer_path}")
        except Exception as e:
            # If loading from local path fails, try HuggingFace as fallback
            if tokenizer_path == SUPPORTED_MODELS[model_id]["local_path"]:
                logger.warning(f"⚠ Failed to load from local path: {e}")
                logger.info(f"  Attempting to load from HuggingFace as fallback...")
                try:
                    hf_path = SUPPORTED_MODELS[model_id]["hf_path"]
                    tokenizers[model_id] = AutoTokenizer.from_pretrained(hf_path)
                    logger.info(f"✓ {model_id} tokenizer loaded successfully from HuggingFace: {hf_path}")
                except Exception as hf_error:
                    logger.error(f"✗ Failed to load {model_id} from HuggingFace: {hf_error}")
                    raise
            else:
                logger.error(f"✗ Failed to load {model_id}: {e}")
                raise
    
    logger.info(f"✓ All tokenizers loaded successfully. Available models: {list(tokenizers.keys())}")
    
    yield
    
    # Shutdown: Cleanup if needed
    logger.info("Shutting down tokenizer service...")

app = FastAPI(
    title="Tokenizer API",
    description="""The Tokenizer API is responsible for text and code preprocessing operations.
It converts raw input (natural language or source code) into tokenized representations compatible with AI models, and can also perform the reverse process to reconstruct text from tokens.
This service ensures consistency in how inputs are prepared across different models, handling encoding, decoding, normalization, and vocabulary lookups.""",
    version="1.0.0",
    lifespan=lifespan
)

@app.post("/tokenize", response_model=TokenizeResponse)
async def tokenize(request: TokenizeRequest):
    """
    Tokenize text using the specified model's tokenizer
    
    Args:
        text: Input text to tokenize
        max_length: Maximum sequence length (default: 512)
        model: Model ID to use for tokenization (default: deberta-v3-base-prompt-injection-v2)
    
    Returns:
        input_ids: Token IDs
        attention_mask: Attention mask
        token_count: Number of actual tokens (before padding)
        model: Model ID that was used
    """
    try:
        # Validate model
        if request.model not in tokenizers:
            raise HTTPException(
                status_code=400, 
                detail=f"Model '{request.model}' not supported. Available models: {list(tokenizers.keys())}"
            )
        
        tokenizer = tokenizers[request.model]
        
        if tokenizer is None:
            raise HTTPException(status_code=503, detail=f"Tokenizer for '{request.model}' not initialized")
        
        # Tokenize with the specified model's tokenizer
        encoded = tokenizer(
            request.text,
            max_length=request.max_length,
            padding="max_length",
            truncation=True,
            return_tensors="pt"
        )
        
        # Convert to lists
        input_ids = encoded["input_ids"][0].tolist()
        attention_mask = encoded["attention_mask"][0].tolist()
        
        # Count actual tokens (non-padding)
        token_count = sum(attention_mask)
        
        logger.info(f"Tokenized text (length: {len(request.text)}) -> {token_count} tokens using {request.model}")
        
        return TokenizeResponse(
            input_ids=input_ids,
            attention_mask=attention_mask,
            token_count=token_count,
            model=request.model
        )
    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"Error during tokenization: {e}")
        raise HTTPException(status_code=500, detail=f"Tokenization failed: {str(e)}")

@app.get("/health", response_model=HealthResponse)
async def health():
    """Health check endpoint"""
    if not tokenizers:
        raise HTTPException(status_code=503, detail="Tokenizers not initialized")
    
    return HealthResponse(
        status="healthy",
        available_models=list(tokenizers.keys()),
        version="1.0.0"
    )

if __name__ == "__main__":
    uvicorn.run(
        app,
        host="0.0.0.0",
        port=8000,
        log_level="info"
    )

