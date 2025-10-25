# To push version v2 of all images: MODEL_VERSION=v2 make push-all
MODEL_VERSION := v1
REGISTRY := ghcr.io/rivoli-ai
PLATFORMS := linux/amd64,linux/arm64

.PHONY: setup-buildx push-model-assets push-inference-service push-tokenizer-service push-all

# Setup multi-platform builder
setup-buildx:
	docker buildx create --name multiplatform --use || docker buildx use multiplatform
	docker buildx inspect --bootstrap

push-model-assets: TAGS ?= $(REGISTRY)/andy-model-assets:$(MODEL_VERSION) $(REGISTRY)/andy-model-assets:latest
push-model-assets: DOCKERFILE := Dockerfile.model-assets
push-model-assets: CONTEXT := .
push-model-assets: LABELS ?=
push-model-assets: LABEL_ARGS := $(if $(LABELS),$(addprefix --label ,$(LABELS)))
push-model-assets: setup-buildx
	docker buildx build --platform $(PLATFORMS) $(LABEL_ARGS) \
		$(addprefix -t ,$(TAGS)) \
		-f $(DOCKERFILE) \
		--push $(CONTEXT)

push-inference-service: TAGS ?= $(REGISTRY)/andy-inference-service:$(MODEL_VERSION) $(REGISTRY)/andy-inference-service:latest
push-inference-service: DOCKERFILE := Dockerfile
push-inference-service: CONTEXT := .
push-inference-service: LABELS ?=
push-inference-service: LABEL_ARGS := $(if $(LABELS),$(addprefix --label ,$(LABELS)))
push-inference-service: setup-buildx
	docker buildx build --platform $(PLATFORMS) $(LABEL_ARGS) \
		$(addprefix -t ,$(TAGS)) \
		-f $(DOCKERFILE) \
		--push $(CONTEXT)

push-tokenizer-service: TAGS ?= $(REGISTRY)/andy-tokenizer-service:$(MODEL_VERSION) $(REGISTRY)/andy-tokenizer-service:latest
push-tokenizer-service: DOCKERFILE := tokenizer-service/Dockerfile
push-tokenizer-service: CONTEXT := tokenizer-service
push-tokenizer-service: LABELS ?=
push-tokenizer-service: LABEL_ARGS := $(if $(LABELS),$(addprefix --label ,$(LABELS)))
push-tokenizer-service: setup-buildx
	docker buildx build --platform $(PLATFORMS) $(LABEL_ARGS) \
		$(addprefix -t ,$(TAGS)) \
		-f $(DOCKERFILE) \
		--push $(CONTEXT)

push-all: push-model-assets push-inference-service push-tokenizer-service
