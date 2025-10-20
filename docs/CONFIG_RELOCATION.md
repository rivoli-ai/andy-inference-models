# Configuration Relocation

## 📦 What Changed

The `models.json` configuration file has been moved to a dedicated `config/` directory for better organization.

## 📂 New Structure

### Before
```
andy-inference-models/
├── scripts/
│   ├── convert_to_onnx.py
│   ├── download_missing_models.py
│   └── models.json                  ← Configuration mixed with scripts
└── models/
```

### After
```
andy-inference-models/
├── config/
│   └── models.json                  ← Dedicated configuration directory
├── scripts/
│   ├── convert_to_onnx.py
│   └── download_missing_models.py
└── models/
```

## ✅ Benefits

1. **Better Organization**
   - Configuration separated from code
   - Clear directory structure
   - Standard practice (config/ is conventional)

2. **Easier to Find**
   - Configuration files in one place
   - Intuitive location for settings
   - Easier for DevOps/config management

3. **Scalability**
   - Can add more config files easily
   - `config/models.json`
   - `config/settings.json`
   - `config/environments/...`

4. **Docker-Friendly**
   - Can mount entire config directory as volume
   - Easy to override config in different environments
   - Clear separation of concerns

## 🔄 Migration

### Automatic Migration

The scripts automatically handle both locations for backward compatibility:

1. **Try new location first**: `config/models.json`
2. **Fallback to old location**: `scripts/models.json` (for backward compatibility)

This means **no manual migration required**! The system works out of the box.

### Manual Migration (if needed)

If you have a custom setup:

```bash
# Create config directory
mkdir config

# Move the file
mv scripts/models.json config/models.json

# Update any custom scripts/tools to look in config/
```

## 📝 Updated References

All documentation and code has been updated to reference `config/models.json`:

- ✅ `scripts/download_missing_models.py` - Updated default path
- ✅ `scripts/convert_to_onnx.py` - Updated config loading
- ✅ `Dockerfile` - Copies `config/` directory
- ✅ `Dockerfile.with-models` - Copies `config/` directory
- ✅ `scripts/README.md` - Updated all references
- ✅ All documentation - Updated paths

## 🐳 Docker Impact

### Dockerfiles Updated

Both Dockerfiles now copy the config directory:

```dockerfile
# Copy all conversion scripts and configuration
WORKDIR /workspace
COPY scripts/ ./scripts/
COPY config/ ./config/        ← Added
```

### No Breaking Changes

- ✅ Existing Docker images will continue to work
- ✅ New builds will use the new location
- ✅ Fallback ensures compatibility

## 📍 Where to Find Configuration

### Development
```bash
# Edit configuration
vi config/models.json
nano config/models.json
code config/models.json
```

### Docker Container
```bash
# Configuration is at project root
/workspace/config/models.json

# Or relative to scripts
../config/models.json
```

### Environment Variable Override (Future)

In the future, you could override the config location:

```bash
export MODELS_CONFIG=/custom/path/models.json
docker run -e MODELS_CONFIG=/config/custom-models.json ...
```

## 🎯 Best Practices

### 1. Keep Config Separate
```
config/          ← Configuration (editable)
scripts/         ← Code (don't edit unless developing)
models/          ← Data (generated)
```

### 2. Version Control
```bash
# Commit config changes
git add config/models.json
git commit -m "feat: add new model configuration"
```

### 3. Environment-Specific Configs
```
config/
├── models.json                 ← Default/production
├── models.development.json     ← Development
└── models.test.json           ← Testing
```

### 4. Docker Volumes
```bash
# Mount custom config
docker run -v $(pwd)/custom-config:/workspace/config myimage

# Or specific file
docker run -v $(pwd)/custom-models.json:/workspace/config/models.json myimage
```

## 🔍 Verification

### Check File Location
```bash
# Verify new location exists
ls -la config/models.json

# Test script can find it
python scripts/download_missing_models.py --check-only
```

### Expected Output
```
[OK] Loaded configuration from .../config/models.json
  Found 2 model(s) defined
Models to download: 0
```

## 📚 Related Documentation

- `scripts/README.md` - How to use the scripts
- `docs/UNIFIED_CONVERTER.md` - Generic converter documentation
- `docs/JSON_CONFIG_MIGRATION.md` - JSON configuration system

## ✨ Summary

**The configuration file is now at `config/models.json`!**

- ✅ More organized project structure
- ✅ Standard configuration directory
- ✅ Backward compatible (automatic fallback)
- ✅ All scripts and docs updated
- ✅ Docker builds updated
- ✅ No manual migration needed

Just remember: **Edit `config/models.json` to add new models!** 🚀


