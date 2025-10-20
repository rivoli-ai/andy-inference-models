FROM python:3.11-slim

WORKDIR /app

# Install dependencies
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Copy application
COPY app.py .

# Create config directory (will be populated via volume mount or build context)
RUN mkdir -p /app/config

# Note: config/models.json will be provided by:
# - Volume mount in docker-compose.yml (for development)
# - Or copied during build if config is in the build context
# The app.py will load models configuration dynamically at startup

# Expose port
EXPOSE 8000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD python -c "import requests; requests.get('http://localhost:8000/health')" || exit 1

# Run the application
CMD ["python", "app.py"]

