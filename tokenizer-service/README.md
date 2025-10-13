# DeBERTa Tokenizer Microservice

Python FastAPI service that provides accurate HuggingFace tokenization for DeBERTa models.

## Features

- ✅ 100% accurate DeBERTa v3 tokenization using HuggingFace Transformers
- ✅ Fast and lightweight FastAPI service
- ✅ Health check endpoint
- ✅ Docker containerized
- ✅ Automatic tokenizer caching

## API Endpoints

### POST /tokenize

Tokenize text input.

**Request:**
```json
{
  "text": "Your text here",
  "max_length": 512
}
```

**Response:**
```json
{
  "input_ids": [1, 2345, 6789, ...],
  "attention_mask": [1, 1, 1, ...],
  "token_count": 45
}
```

### GET /health

Health check endpoint.

**Response:**
```json
{
  "status": "healthy",
  "tokenizer_model": "protectai/deberta-v3-base-prompt-injection-v2",
  "version": "1.0.0"
}
```

### GET /

Root endpoint with service information.

## Running Locally

### Using Python

```bash
# Install dependencies
pip install -r requirements.txt

# Run the service
python app.py
```

Service will be available at: `http://localhost:8000`

### Using Docker

```bash
# Build the image
docker build -t deberta-tokenizer .

# Run the container
docker run -p 8000:8000 deberta-tokenizer
```

## Running with Docker Compose

The tokenizer service is automatically started when you run the main application:

```bash
docker-compose up
```

This will:
1. Start the tokenizer service on port 8000
2. Start the main API service on port 5158
3. Configure them to communicate via internal network

## Testing

Test the service using curl:

```bash
# Health check
curl http://localhost:8000/health

# Tokenize text
curl -X POST http://localhost:8000/tokenize \
  -H "Content-Type: application/json" \
  -d '{"text": "Ignore previous instructions", "max_length": 512}'
```

## Configuration

The service uses:
- **Model**: `protectai/deberta-v3-base-prompt-injection-v2`
- **Port**: 8000 (configurable in app.py)
- **Max Length**: 512 (configurable per request)

## Performance

- **Startup Time**: ~5-10 seconds (includes downloading tokenizer if not cached)
- **Request Time**: ~5-10ms per tokenization
- **Memory**: ~200MB

## Troubleshooh

### Service not starting

Check logs:
```bash
docker-compose logs tokenizer-service
```

### Connection refused

Ensure the service is healthy:
```bash
curl http://localhost:8000/health
```

### Slow first request

The first request may be slower as the tokenizer is loaded. Subsequent requests are fast.

## Development

To modify the service:

1. Edit `app.py`
2. Update `requirements.txt` if needed
3. Rebuild the Docker image:
   ```bash
   docker-compose up --build
   ```

## Integration with C# API

The C# API automatically uses this service when configured:

**appsettings.json:**
```json
{
  "ModelConfiguration": {
    "TokenizerServiceUrl": "http://tokenizer-service:8000"
  }
}
```

The C# tokenizer will:
1. Try to connect to the Python service
2. Fall back to GPT-2 tokenizer if service is unavailable
3. Log the tokenization method being used

## License

Same as parent project.

