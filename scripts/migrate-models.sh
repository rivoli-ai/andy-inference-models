#!/bin/bash
# Bash script to migrate model files to organized folder structure
# Run this script from the project root directory

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
GRAY='\033[0;37m'
NC='\033[0m' # No Color

# Default values
MODEL_ID="${1:-deberta-v3-base-prompt-injection-v2}"
DRY_RUN=false

# Parse arguments
while [[ "$#" -gt 0 ]]; do
    case $1 in
        --dry-run) DRY_RUN=true ;;
        --model-id) MODEL_ID="$2"; shift ;;
        *) MODEL_ID="$1" ;;
    esac
    shift
done

echo -e "${CYAN}=====================================${NC}"
echo -e "${CYAN}Model Folder Migration Script${NC}"
echo -e "${CYAN}=====================================${NC}"
echo ""

# Check if models directory exists
if [ ! -d "models" ]; then
    echo -e "${RED}Error: models directory not found!${NC}"
    echo -e "${YELLOW}Please run this script from the project root directory.${NC}"
    exit 1
fi

TARGET_FOLDER="models/$MODEL_ID"

# Check if target folder already exists
if [ -d "$TARGET_FOLDER" ]; then
    echo -e "${YELLOW}Warning: Target folder '$TARGET_FOLDER' already exists!${NC}"
    read -p "Do you want to continue? (y/n) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo -e "${YELLOW}Migration cancelled.${NC}"
        exit 0
    fi
else
    if [ "$DRY_RUN" = true ]; then
        echo -e "${YELLOW}[DRY RUN] Would create folder: $TARGET_FOLDER${NC}"
    else
        echo -e "${GREEN}Creating folder: $TARGET_FOLDER${NC}"
        mkdir -p "$TARGET_FOLDER"
    fi
fi

echo ""
echo -e "${CYAN}Looking for model files in 'models/' root...${NC}"

# Find all model files (excluding directories and scripts)
MODEL_FILES=()
while IFS= read -r -d '' file; do
    filename=$(basename "$file")
    # Skip README, scripts, and directories
    if [[ "$filename" != "README.md" && "$filename" != ".gitkeep" && -f "$file" ]]; then
        case "${filename##*.}" in
            onnx|json|model|txt)
                MODEL_FILES+=("$file")
                ;;
        esac
    fi
done < <(find models -maxdepth 1 -type f -print0)

if [ ${#MODEL_FILES[@]} -eq 0 ]; then
    echo -e "${YELLOW}No model files found in 'models/' root directory.${NC}"
    echo -e "${YELLOW}Files might already be organized or located elsewhere.${NC}"
    exit 0
fi

echo -e "${GREEN}Found ${#MODEL_FILES[@]} files to migrate:${NC}"
for file in "${MODEL_FILES[@]}"; do
    echo -e "  ${GRAY}- $(basename "$file")${NC}"
done

echo ""

if [ "$DRY_RUN" = true ]; then
    echo -e "${YELLOW}[DRY RUN] Would move the following files:${NC}"
    for file in "${MODEL_FILES[@]}"; do
        destination="$TARGET_FOLDER/$(basename "$file")"
        echo -e "  ${GRAY}$file -> $destination${NC}"
    done
    echo ""
    echo -e "${YELLOW}[DRY RUN] No files were actually moved.${NC}"
    echo -e "${CYAN}Run without --dry-run parameter to perform the migration.${NC}"
else
    read -p "Proceed with migration? (y/n) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo -e "${YELLOW}Migration cancelled.${NC}"
        exit 0
    fi
    
    echo ""
    echo -e "${CYAN}Migrating files...${NC}"
    
    SUCCESS_COUNT=0
    ERROR_COUNT=0
    
    for file in "${MODEL_FILES[@]}"; do
        filename=$(basename "$file")
        destination="$TARGET_FOLDER/$filename"
        
        if mv "$file" "$destination" 2>/dev/null; then
            echo -e "  ${GREEN}✓ Moved: $filename${NC}"
            ((SUCCESS_COUNT++))
        else
            echo -e "  ${RED}✗ Failed: $filename${NC}"
            ((ERROR_COUNT++))
        fi
    done
    
    echo ""
    echo -e "${CYAN}=====================================${NC}"
    echo -e "${CYAN}Migration Summary${NC}"
    echo -e "${CYAN}=====================================${NC}"
    echo -e "${GREEN}Successfully moved: $SUCCESS_COUNT files${NC}"
    if [ $ERROR_COUNT -gt 0 ]; then
        echo -e "${RED}Failed to move: $ERROR_COUNT files${NC}"
    fi
    echo ""
    echo -e "${CYAN}Model files are now in: $TARGET_FOLDER${NC}"
    echo ""
    echo -e "${YELLOW}Next steps:${NC}"
    echo -e "${GRAY}1. Update appsettings.json to include:${NC}"
    echo -e "${GRAY}   \"ModelFolder\": \"$TARGET_FOLDER\"${NC}"
    echo -e "${GRAY}2. Restart your application${NC}"
    echo -e "${GRAY}3. Test with: GET /$MODEL_ID/health${NC}"
    echo ""
fi

