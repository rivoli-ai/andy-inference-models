MODEL_VERSION := v1
REGISTRY := ghcr.io/rivoli-ai
PLATFORMS := linux/amd64,linux/arm64,windows/amd64

.PHONY: build-models push-models build-multiplatform push-multiplatform setup-buildx all

# Setup multi-platform builder
setup-buildx:
	docker buildx create --name multiplatform --use || docker buildx use multiplatform
	docker buildx inspect --bootstrap

# Build single platform (current behavior)
build-models:
	docker build -t $(REGISTRY)/andy-model-assets:$(MODEL_VERSION) -f Dockerfile.model-assets .

# Build multi-platform images
build-multiplatform:
	docker buildx build --platform $(PLATFORMS) \
		-t $(REGISTRY)/andy-model-assets:$(MODEL_VERSION) \
		-t $(REGISTRY)/andy-model-assets:latest \
		-f Dockerfile.model-assets \
		--load .

# Build and push multi-platform images
push-multiplatform:
	docker buildx build --platform $(PLATFORMS) \
		-t $(REGISTRY)/andy-model-assets:$(MODEL_VERSION) \
		-t $(REGISTRY)/andy-model-assets:latest \
		-f Dockerfile.model-assets \
		--push .

# Push single platform (current behavior)
push-models:
	docker push $(REGISTRY)/andy-model-assets:$(MODEL_VERSION)

# Build all services multi-platform
build-all-multiplatform:
	docker buildx build --platform $(PLATFORMS) \
		-t $(REGISTRY)/andy-inference-service:$(MODEL_VERSION) \
		-t $(REGISTRY)/andy-inference-service:latest \
		-f Dockerfile \
		--load .
	docker buildx build --platform $(PLATFORMS) \
		-t $(REGISTRY)/andy-tokenizer-service:$(MODEL_VERSION) \
		-t $(REGISTRY)/andy-tokenizer-service:latest \
		-f tokenizer-service/Dockerfile \
		--load .

# Push all services multi-platform
push-all-multiplatform:
	docker buildx build --platform $(PLATFORMS) \
		-t $(REGISTRY)/andy-inference-service:$(MODEL_VERSION) \
		-t $(REGISTRY)/andy-inference-service:latest \
		-f Dockerfile \
		--push .
	docker buildx build --platform $(PLATFORMS) \
		-t $(REGISTRY)/andy-tokenizer-service:$(MODEL_VERSION) \
		-t $(REGISTRY)/andy-tokenizer-service:latest \
		-f tokenizer-service/Dockerfile \
		--push .

all: build-models push-models
all-multiplatform: build-all-multiplatform push-all-multiplatform
