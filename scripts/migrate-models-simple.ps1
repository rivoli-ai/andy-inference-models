# Simple PowerShell script to migrate model files
param(
    [string]$ModelId = "deberta-v3-base-prompt-injection-v2"
)

$ErrorActionPreference = "Stop"

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Model Folder Migration Script" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

$targetFolder = "models\$ModelId"

# Create target folder
if (!(Test-Path $targetFolder)) {
    Write-Host "Creating folder: $targetFolder" -ForegroundColor Green
    New-Item -ItemType Directory -Path $targetFolder -Force | Out-Null
}

# Get model files
$files = @(
    "model.onnx",
    "tokenizer.json",
    "config.json",
    "special_tokens_map.json",
    "tokenizer_config.json",
    "added_tokens.json",
    "spm.model"
)

Write-Host "Migrating files..." -ForegroundColor Cyan
$successCount = 0

foreach ($file in $files) {
    $sourcePath = "models\$file"
    if (Test-Path $sourcePath) {
        $destPath = "$targetFolder\$file"
        try {
            Move-Item -Path $sourcePath -Destination $destPath -Force
            Write-Host "  Moved: $file" -ForegroundColor Green
            $successCount++
        } catch {
            Write-Host "  Failed: $file" -ForegroundColor Red
        }
    }
}

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Migration Complete!" -ForegroundColor Green
Write-Host "Successfully moved: $successCount files" -ForegroundColor Green
Write-Host "Model files are now in: $targetFolder" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan

