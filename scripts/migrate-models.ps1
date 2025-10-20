# PowerShell script to migrate model files to organized folder structure
# Run this script from the project root directory

param(
    [Parameter(Mandatory=$false)]
    [string]$ModelId = "deberta-v3-base-prompt-injection-v2",
    
    [Parameter(Mandatory=$false)]
    [switch]$DryRun = $false
)

$ErrorActionPreference = "Stop"

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Model Folder Migration Script" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Check if models directory exists
if (!(Test-Path "models")) {
    Write-Host "Error: models directory not found!" -ForegroundColor Red
    Write-Host "Please run this script from the project root directory." -ForegroundColor Yellow
    exit 1
}

$targetFolder = "models/$ModelId"

# Check if target folder already exists
if (Test-Path $targetFolder) {
    Write-Host "Warning: Target folder '$targetFolder' already exists!" -ForegroundColor Yellow
    $response = Read-Host "Do you want to continue? (y/n)"
    if ($response -ne "y") {
        Write-Host "Migration cancelled." -ForegroundColor Yellow
        exit 0
    }
} else {
    if ($DryRun) {
        Write-Host "[DRY RUN] Would create folder: $targetFolder" -ForegroundColor Yellow
    } else {
        Write-Host "Creating folder: $targetFolder" -ForegroundColor Green
        New-Item -ItemType Directory -Path $targetFolder -Force | Out-Null
    }
}

Write-Host ""
Write-Host "Looking for model files in 'models/' root..." -ForegroundColor Cyan

# Get all model files (excluding directories and scripts)
$modelFiles = Get-ChildItem -Path "models" -File | Where-Object {
    $_.Extension -in @('.onnx', '.json', '.model', '.txt') -and
    $_.Name -notin @('README.md', '.gitkeep')
}

if ($modelFiles.Count -eq 0) {
    Write-Host "No model files found in 'models/' root directory." -ForegroundColor Yellow
    Write-Host "Files might already be organized or located elsewhere." -ForegroundColor Yellow
    exit 0
}

Write-Host "Found $($modelFiles.Count) files to migrate:" -ForegroundColor Green
foreach ($file in $modelFiles) {
    Write-Host "  - $($file.Name)" -ForegroundColor Gray
}

Write-Host ""

if ($DryRun) {
    Write-Host "[DRY RUN] Would move the following files:" -ForegroundColor Yellow
    foreach ($file in $modelFiles) {
        $destination = Join-Path $targetFolder $file.Name
        Write-Host "  $($file.FullName) -> $destination" -ForegroundColor Gray
    }
    Write-Host ""
    Write-Host "[DRY RUN] No files were actually moved." -ForegroundColor Yellow
    Write-Host "Run without -DryRun parameter to perform the migration." -ForegroundColor Cyan
} else {
    $response = Read-Host "Proceed with migration? (y/n)"
    if ($response -ne "y") {
        Write-Host "Migration cancelled." -ForegroundColor Yellow
        exit 0
    }
    
    Write-Host ""
    Write-Host "Migrating files..." -ForegroundColor Cyan
    
    $successCount = 0
    $errorCount = 0
    
    foreach ($file in $modelFiles) {
        try {
            $destination = Join-Path $targetFolder $file.Name
            Move-Item -Path $file.FullName -Destination $destination -Force
            Write-Host "  ✓ Moved: $($file.Name)" -ForegroundColor Green
            $successCount++
        } catch {
            Write-Host "  ✗ Failed: $($file.Name) - $($_.Exception.Message)" -ForegroundColor Red
            $errorCount++
        }
    }
    
    Write-Host ""
    Write-Host "=====================================" -ForegroundColor Cyan
    Write-Host "Migration Summary" -ForegroundColor Cyan
    Write-Host "=====================================" -ForegroundColor Cyan
    Write-Host "Successfully moved: $successCount files" -ForegroundColor Green
    if ($errorCount -gt 0) {
        Write-Host "Failed to move: $errorCount files" -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "Model files are now in: $targetFolder" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "1. Update appsettings.json to include:" -ForegroundColor Gray
    Write-Host "   `"ModelFolder`": `"$targetFolder`"" -ForegroundColor Gray
    Write-Host "2. Restart your application" -ForegroundColor Gray
    Write-Host "3. Test with: GET /$ModelId/health" -ForegroundColor Gray
    Write-Host ""
}

