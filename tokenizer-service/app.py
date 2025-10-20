"""
Tokenizer Microservice
FastAPI service that provides accurate HuggingFace tokenization for AI models
"""
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from transformers import AutoTokenizer
import uvicorn
import logging
from typing import List

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

app = FastAPI(
    title="Tokenizer API",
    description="""The Tokenizer API is responsible for text and code preprocessing operations.
It converts raw input (natural language or source code) into tokenized representations compatible with AI models, and can also perform the reverse process to reconstruct text from tokens.
This service ensures consistency in how inputs are prepared across different models, handling encoding, decoding, normalization, and vocabulary lookups.""",
    version="1.0.0"
)

# Request/Response models
class TokenizeRequest(BaseModel):
    text: str
    max_length: int = 512
    
class TokenizeResponse(BaseModel):
    input_ids: List[int]
    attention_mask: List[int]
    token_count: int

class HealthResponse(BaseModel):
    status: str
    tokenizer_model: str
    version: str

# Global tokenizer instance (loaded once at startup)
tokenizer = None

@app.on_event("startup")
async def startup_event():
    """Load tokenizer at startup"""
    global tokenizer
    try:
        logger.info("Loading DeBERTa tokenizer from HuggingFace...")
        tokenizer = AutoTokenizer.from_pretrained(
            "protectai/deberta-v3-base-prompt-injection-v2"
        )
        logger.info("✓ Tokenizer loaded successfully")
    except Exception as e:
        logger.error(f"✗ Failed to load tokenizer: {e}")
        raise

@app.post("/tokenize", response_model=TokenizeResponse)
async def tokenize(request: TokenizeRequest):
    """
    Tokenize text using DeBERTa tokenizer
    
    Args:
        text: Input text to tokenize
        max_length: Maximum sequence length (default: 512)
    
    Returns:
        input_ids: Token IDs
        attention_mask: Attention mask
        token_count: Number of actual tokens (before padding)
    """
    try:
        if tokenizer is None:
            raise HTTPException(status_code=503, detail="Tokenizer not initialized")
        
        # Tokenize with proper DeBERTa tokenization
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
        
        logger.info(f"Tokenized text (length: {len(request.text)}) -> {token_count} tokens")
        
        return TokenizeResponse(
            input_ids=input_ids,
            attention_mask=attention_mask,
            token_count=token_count
        )
    except Exception as e:
        logger.error(f"Error during tokenization: {e}")
        raise HTTPException(status_code=500, detail=f"Tokenization failed: {str(e)}")

@app.get("/health", response_model=HealthResponse)
async def health():
    """Health check endpoint"""
    if tokenizer is None:
        raise HTTPException(status_code=503, detail="Tokenizer not initialized")
    
    return HealthResponse(
        status="healthy",
        tokenizer_model="protectai/deberta-v3-base-prompt-injection-v2",
        version="1.0.0"
    )

if __name__ == "__main__":
    uvicorn.run(
        app,
        host="0.0.0.0",
        port=8000,
        log_level="info"
    )

