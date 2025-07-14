# Pandora Build Utility Documentation

A comprehensive Unity Editor utility for streamlined build management, supporting both single-project and multi-variant (wildcard) build configurations.

## Features

### 🎯 **Dual Build Modes**
- **Single Mode**: Traditional single-project builds with multiple profiles
- **Wildcard Mode**: Multi-variant builds with different code names and configurations

### 🧙‍♂️ **Setup Wizard**
- Interactive step-by-step configuration wizard
- Automatic validation and error prevention
- Scrollable interface for complex configurations
- Smart defaults for quick setup

### 🏗️ **Build Profiles**
- Multiple build profiles per project/variant
- Configurable build targets (Windows, macOS, Linux, etc.)
- Development and production build settings
- Custom build suffixes and product names
- Build folder management

### 🎮 **Wildcard Mode Features**
- Multiple code name variants in a single project
- Automatic scripting define symbols management
- Per-variant scene configurations
- Per-variant build profiles
- Easy switching between variants

### 🔧 **Advanced Configuration**
- Version management with auto-increment
- Scene validation and management
- Build folder path configuration
- Custom build icons support
- NUnit test project compatibility

## Quick Start

### 1. Open Build Settings Utility
- Go to `Window` → `Pandora` → `Build Settings`
- The setup wizard will launch automatically on first use

### 2. Choose Your Build Mode

**Single Mode**: Best for projects with one main variant
- Configure project name and settings
- Add scenes for building
- Create build profiles (Development, Production, etc.)

**Wildcard Mode**: Best for projects with multiple variants
- Configure multiple code names (e.g., "GameA", "GameB", "GameC")
- Set up scenes and build profiles per variant
- Automatic scripting define symbols management

### 3. Configure Build Settings
- **Build Folder**: Choose where builds will be saved
- **Scenes**: Select which scenes to include in builds
- **Build Profiles**: Configure different build types
- **Version**: Set and manage version numbers

### 4. Start Building
- Use the main interface to switch between variants (Wildcard mode)
- Build individual profiles or all profiles at once
- Monitor build progress and results

## Usage Examples

Direct programmatic access to the build settings is not typically required, as all configuration can be managed through the editor window at `Window` → `Pandora` → `Build Settings`.
However, if you need to interact with the settings via a script, you can refer to the usage examples below.

### Single Mode Configuration
```csharp
// Example build profile for single mode
var productionProfile = new BuildProfile
{
    ProfileName = "MyGame Production",
    DisplayName = "Production",
    BuildSuffix = "prod",
    ProductName = "MyGame",
    BuildTarget = BuildTarget.StandaloneWindows64,
    Development = false,
    ScriptDebugging = false
};
```

### Wildcard Mode Configuration
```csharp
// Example code name configuration
var gameAConfig = new CodeNameConfig
{
    CodeName = "GameA",
    ScriptDefineSymbol = "GAME_A",
    Scenes = new List<SceneAsset> { /* your scenes */ },
    BuildProfiles = new List<BuildProfile> { /* your profiles */ }
};
```
```