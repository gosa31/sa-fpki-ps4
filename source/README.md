# FPKGi

FPKGi is a Unity-based PS4 homebrew content manager and installer with a modern PS4-style UI.

## Overview

- Unity project for a modern PS4-style UI
- Automatic PS4 PKG generation using real PS4 packaging format
- Supports homebrew games, emulators, and custom content
- Modern sidebar menu with sorting, filtering, and content type selection
- Direct download and installation capability

## Features

- **Real PS4 PKG Output**: Creates actual installable `.pkg` files for jailbroken/devkit PS4
- **Built-in Package Creator**: `create_pkg_builder.py` generates real PS4 packages automatically
- **Game Library Management**: JSON-based game metadata system
- **Modern UI**: PS4-style interface with smooth navigation
- **Multiple Content Types**: Games, Apps, Emulators, Themes, DLC, and Homebrew
- **Region Filtering**: Filter content by USA, Europe, Japan, or Asia regions
- **User Preferences**: Save sorting options, preferred regions, and download settings

## What is ready

- FPKGi application is fully functional with a modern UI
- `create_pkg_builder.py` generates real, installable PS4 `.pkg` files
- GitHub Actions workflow automatically builds and generates PKG on each push
- Real PS4 executable structure with param.sfo and eboot.bin

## Project Structure

```
.
├── Assets/                 # Game assets (images, fonts, audio)
├── Builds/PS4/             # PS4 build outputs
├── DATA/ContentJSONs/      # User-provided game metadata (JSON files)
├── create_pkg_builder.py   # Real PS4 PKG creator
├── build_pkg.py            # Alternative packaging script
├── convert_to_pkg.py       # PS4 build converter
├── ps4_build_config.json   # Build configuration
└── FPKGi.sln              # Unity solution
```

## Getting Started

### Prerequisites
- Unity 2021 LTS or newer
- Python 3.10+
- Git

### Setup

1. Clone the repository:
   ```bash
   git clone https://github.com/gosa31/sa-fpki-ps4.git
   cd sa-fpki-ps4/source
   ```

2. Add your game metadata to `DATA/ContentJSONs/`:
   Create `GAMES.json` with your game list (see example below)

3. Open the project in Unity and run the scene

### Creating a PKG Locally

```bash
python create_pkg_builder.py
```

This generates:
- `build/FPKGi_v2.0.0_PS4.pkg` - Real PS4 package file
- `build/FPKGi_v2.0.0_PS4.sha256` - SHA256 checksum

### Game Metadata Format

Create `DATA/ContentJSONs/GAMES.json`:

```json
{
  "DATA": {
    "https://example.com/game.pkg": {
      "title_id": "CUSA12345",
      "region": "USA",
      "name": "Game Title",
      "version": "01.00",
      "release": "01-15-2024",
      "size": "50000000000",
      "min_fw": "5.05",
      "cover_url": "https://example.com/cover.png"
    }
  }
}
```

## GitHub Actions

The repository includes a GitHub Actions workflow that:
- Builds the project automatically on each push
- Generates a real PS4 PKG file
- Creates SHA256 checksums
- Uploads artifacts for download

To use:
1. Push to `main` branch
2. Check Actions for build status
3. Download the `.pkg` file from artifacts

## Installation on PS4

1. Transfer the `.pkg` file to a USB drive
2. On a jailbroken/devkit PS4:
   - Go to Settings > System Software > Install Package Files
   - Select the `.pkg` file from USB
   - Follow the installation prompts

## Configuration

Edit `ps4_build_config.json` to customize:
- App ID and title
- Build settings and optimization levels
- Package metadata and output paths
- Directory mappings for assets

## Notes

- This is a homebrew application for jailbroken or developer PS4 systems
- Requires proper PS4 firmware version (5.05 or later recommended)
- Content metadata must be provided by the user
- The application respects user preferences and regions
