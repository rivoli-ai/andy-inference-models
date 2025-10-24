MODEL_VERSION := v1
REGISTRY := ghcr.io/rivoli-ai

.PHONY: build-models push-models all

build-models:
	docker build -t $(REGISTRY)/andy-model-assets:$(MODEL_VERSION) -f Dockerfile.model-assets .

push-models:
	docker push $(REGISTRY)/andy-model-assets:$(MODEL_VERSION)

all: build-models push-models
