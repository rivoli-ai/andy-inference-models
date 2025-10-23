# GHCR Publishing & Testcontainers

## 🔄 CI Workflow Overview
- Workflow: `.github/workflows/publish-images.yml`
- Triggers:
  - Pushes to `main` → builds `latest`, `main`, and `sha` tags
  - Tags matching `v*` → builds semantic version tags (`1.2.3`, `1.2`, `1`)
  - Pull requests → build only (no push) for validation
  - Manual `workflow_dispatch` → build/push on demand
- Images are built with BuildKit and pushed to GitHub Container Registry (GHCR) using the repository `GITHUB_TOKEN` (requires default `packages: write` permission).

## 📦 Image Names & Tags
Replace `<org>` with your GitHub organization or username.

| Service | Image | Notes |
|---------|-------|-------|
| Inference API | `ghcr.io/<org>/andy-inference-service` | Includes ASP.NET API, ONNX models, and config |
| Tokenizer API | `ghcr.io/<org>/andy-tokenizer-service` | FastAPI tokenizer service (mount `/app/config` for custom configs) |

### Versioning scheme
- `latest`: only when the default branch (`main`) builds successfully.
- `main`: tracks the current default branch commit.
- `sha-<short>`: unique tag per commit (handy for debugging).
- `v1.2.3`, `1.2`, `1`: produced when pushing annotated or lightweight tags that begin with `v`.

## 🔐 Registry Access
- Pull public tags directly: `docker pull ghcr.io/<org>/andy-inference-service:<tag>`
- The workflow makes packages public automatically; no authentication is required for consumers.
- To push locally, authenticate once:  
  `echo "$GHCR_TOKEN" | docker login ghcr.io -u <org> --password-stdin`  
  where `GHCR_TOKEN` is a PAT with `packages:write`.

## 🧪 Using the Images with Testcontainers
```csharp
var tokenizer = new ContainerBuilder()
    .WithImage("ghcr.io/<org>/andy-tokenizer-service:1.0.0")
    .WithPortBinding(8000, true)
    .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded("/health"))
    .Build();

var inference = new ContainerBuilder()
    .WithImage("ghcr.io/<org>/andy-inference-service:1.0.0")
    .DependsOn(tokenizer)
    .WithPortBinding(8080, true)
    .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded("/health"))
    .Build();
```

- Testcontainers automatically pulls missing tags; pre-release tags keep fixtures reproducible.
- Use the same tags locally and in CI for deterministic test runs.
- No registry authentication is needed for pulls unless you choose to publish private forks; just ensure pushes use authenticated credentials.

## 🚀 Release Checklist
1. Update version in your release notes or changelog.
2. Tag the repo (`git tag v1.2.3 && git push origin v1.2.3`).
3. Wait for the `Publish Containers` workflow to finish.
4. Verify images with `docker pull ghcr.io/<org>/andy-inference-service:1.2.3`.
5. Update downstream Testcontainers fixtures to the new tag.
