# -*- coding: utf-8 -*-
#!/usr/bin/env python3
"""
FPKGi PS4 Package Builder
Collect build artifacts and create a package archive for later PS4 packaging.
"""

import json
import os
import shutil
import sys
import subprocess
from pathlib import Path
from datetime import datetime

from ps4_pkg_utils import write_param_sfo


class PS4PackageBuilder:
    def __init__(self, project_root):
        self.project_root = Path(project_root)
        self.build_dir = self.project_root / "build"
        self.pkg_dir = self.build_dir / "pkg"
        self.config = self._load_config()
        self.required_dirs = [
            self.project_root / "Assets" / "Resources",
            self.project_root / "Assets" / "Fonts",
            self.project_root / "Assets" / "Images",
            self.project_root / "Assets" / "Audio",
        ]
        self.executable_name = self.config.get("ps4_build_config", {}).get("entry_point", {}).get("binary", "eboot.bin")

    def _load_config(self):
        config_path = self.project_root / "ps4_build_config.json"
        if config_path.exists():
            with open(config_path, encoding="utf-8") as f:
                return json.load(f)
        return {}

    def setup_directories(self):
        print("[*] Setting up build directories...")
        self.pkg_dir.mkdir(parents=True, exist_ok=True)
        (self.pkg_dir / "sce_sys").mkdir(parents=True, exist_ok=True)
        (self.pkg_dir / "app").mkdir(parents=True, exist_ok=True)
        (self.pkg_dir / "app" / "assets").mkdir(parents=True, exist_ok=True)
        (self.pkg_dir / "app" / "fonts").mkdir(parents=True, exist_ok=True)
        (self.pkg_dir / "app" / "scenes").mkdir(parents=True, exist_ok=True)
        print("[OK] Build directories are ready")
        return True

    def validate_required_files(self):
        print("[*] Validating required source files...")
        missing = [str(path) for path in self.required_dirs if not path.exists()]
        if missing:
            print("[!] Warning: Some expected source folders are missing:")
            for path in missing:
                print(f"    - {path}")
            print("[!] The archive can still be created, but the package may be incomplete.")
        else:
            print("[OK] Required source folders are present")
        return True

    def copy_executable(self):
        print("[*] Locating PS4 executable...")
        candidate_paths = [
            self.project_root / self.executable_name,
            self.project_root / "Builds" / "PS4" / self.executable_name,
            self.project_root / "Builds" / self.executable_name,
            self.project_root / "build" / self.executable_name,
            self.project_root / "build" / "pkg" / "app" / self.executable_name,
            self.project_root / "build" / "pkg_contents" / "app" / self.executable_name,
        ]

        for candidate in candidate_paths:
            if candidate.exists():
                if candidate.stat().st_size == 0:
                    print(f"[ERROR] Invalid executable (size 0): {candidate}")
                    continue
                destination = self.pkg_dir / "app" / self.executable_name
                shutil.copy2(candidate, destination)
                print(f"[OK] Copied executable: {candidate}")
                return True

        print(f"[ERROR] Missing or invalid executable: {self.executable_name}")
        print("    Expected one of:")
        for candidate in candidate_paths:
            print(f"      - {candidate}")
        print("[!] Place a real PS4 binary into the project before packaging.")
        return False

    def copy_directories(self):
        print("[*] Copying asset directories...")
        directories = self.config.get("ps4_build_config", {}).get("directories", [
            {"src": "Assets/Resources", "dst": "app/assets"},
            {"src": "Assets/Fonts", "dst": "app/fonts"},
            {"src": "Assets/Images", "dst": "app/images"},
            {"src": "Assets/Audio", "dst": "app/audio"},
            {"src": "Assets/Scenes", "dst": "app/scenes"},
        ])

        copied = False
        for entry in directories:
            src = self.project_root / entry.get("src", "")
            dst_rel = entry.get("dst", "").lstrip("/\\")
            dst = self.pkg_dir / Path(dst_rel)
            if src.exists():
                dst.mkdir(parents=True, exist_ok=True)
                shutil.copytree(src, dst, dirs_exist_ok=True)
                print(f"[OK] Copied {entry.get('src')} -> {entry.get('dst')}")
                copied = True
            else:
                print(f"[!] Source missing: {src}")

        if not copied:
            print("[!] No asset directories were copied. Verify your source folders.")
        return True

    def create_sfo_metadata(self):
        print("[*] Writing SFO metadata files...")
        metadata = self.config.get("ps4_build_config", {}).get("package_metadata", {})
        sfo_data = {
            "APP_TYPE": "Game",
            "ATTRIBUTE": "0",
            "CATEGORY": "gm",
            "CONTENT_ID": f"UP0001-{metadata.get('app_id', 'FPKG00001')}_00",
            "PARENTAL_LEVEL": "3",
            "PUBTOOLINFO": "100",
            "TITLE": metadata.get("title", "FPKGi"),
            "TITLE_ID": metadata.get("app_id", "FPKG00001"),
            "VERSION": metadata.get("version", "02.00"),
            "SYSTEM_VER": "05.050",
        }
        sfo_dir = self.pkg_dir / "sce_sys"
        sfo_dir.mkdir(parents=True, exist_ok=True)
        write_param_sfo(sfo_dir / "param.sfo", sfo_data)
        with open(sfo_dir / "param.sfo.json", "w", encoding="utf-8") as f:
            json.dump(sfo_data, f, indent=2, ensure_ascii=False)
        with open(sfo_dir / "param.sfo.txt", "w", encoding="utf-8") as f:
            for key, value in sfo_data.items():
                f.write(f"{key}={value}\n")
        print("[OK] Binary param.sfo created and metadata exported")
        return True

    def create_manifest(self):
        print("[*] Creating build manifest...")
        config = self.config.get("ps4_build_config", {})
        manifest = {
            "format_version": "1.0",
            "created_at": datetime.now().isoformat(),
            "project": config.get("project", "FPKGi"),
            "version": config.get("version", "2.0.0"),
            "platform": "PS4",
            "files": [],
        }
        for root, _, files in os.walk(self.pkg_dir):
            for filename in files:
                path = Path(root) / filename
                manifest["files"].append(str(path.relative_to(self.pkg_dir)))
        with open(self.pkg_dir / "MANIFEST.json", "w", encoding="utf-8") as f:
            json.dump(manifest, f, indent=2, ensure_ascii=False)
        print(f"[OK] Manifest created with {len(manifest['files'])} entries")
        return True

    def build_archive(self):
        print("[*] Creating archive from package folder...")
        output_cfg = self.config.get("ps4_build_config", {}).get("output", {})
        archive_name = Path(output_cfg.get("pkg_name", "FPKGi_v2.0.0_PS4.zip")).with_suffix(".zip").name
        output_path = self.build_dir / archive_name
        try:
            shutil.make_archive(str(output_path.with_suffix("")), "zip", self.pkg_dir)
            print(f"[OK] Archive created: {archive_name}")
            return True
        except Exception as exc:
            print(f"[ERROR] Failed to create archive: {exc}")
            return False

    def create_pkg_file(self):
        print("[*] Attempting to create PS4 PKG file...")
        output_cfg = self.config.get("ps4_build_config", {}).get("output", {})
        pkg_name = Path(output_cfg.get("pkg_name", "FPKGi_v2.0.0_PS4.zip")).with_suffix(".pkg").name
        pkg_output = self.build_dir / pkg_name
        zip_source = self.build_dir / Path(output_cfg.get("pkg_name", "FPKGi_v2.0.0_PS4.zip")).with_suffix(".zip")

        try:
            result = subprocess.run([
                "orbis-pub-cmd.exe", "img_create",
                "--oformat", "pkg",
                str(self.pkg_dir),
                str(pkg_output)
            ], capture_output=True, text=True, timeout=60)
            if result.returncode == 0:
                print(f"[OK] Created PKG file: {pkg_output.name}")
                return True
            print(f"[!] orbis-pub-cmd failed: {result.stderr}")
        except FileNotFoundError:
            print("[!] orbis-pub-cmd.exe not found. Install Orbis SDK or use alternative tools.")
        except subprocess.TimeoutExpired:
            print("[!] PKG creation timed out.")
        except Exception as exc:
            print(f"[!] PKG creation failed: {exc}")

        try:
            result = subprocess.run([
                "ps4-pkg-tool.exe", "create",
                str(self.pkg_dir),
                str(pkg_output)
            ], capture_output=True, text=True, timeout=60)
            if result.returncode == 0:
                print(f"[OK] Created PKG file with ps4-pkg-tool: {pkg_output.name}")
                return True
            print(f"[!] ps4-pkg-tool failed: {result.stderr}")
        except FileNotFoundError:
            print("[!] ps4-pkg-tool.exe not found.")
        except subprocess.TimeoutExpired:
            print("[!] PKG creation timed out.")
        except Exception as exc:
            print(f"[!] PKG creation failed: {exc}")

        print("[!] Could not create any PKG file. Ensure Orbis SDK or ps4-pkg-tool is installed and the build contains a valid PS4 binary.")
        return False

    def build(self):
        print("\n" + "=" * 60)
        print("  FPKGi PS4 Package Builder")
        print("=" * 60 + "\n")
        required_steps = [
            ("Setup Directories", self.setup_directories),
            ("Validate Sources", self.validate_required_files),
            ("Copy Executable", self.copy_executable),
            ("Copy Assets", self.copy_directories),
            ("Create SFO Metadata", self.create_sfo_metadata),
            ("Create Manifest", self.create_manifest),
            ("Create PKG File", self.create_pkg_file),
            ("Build Archive", self.build_archive),
        ]

        for title, func in required_steps:
            if not func():
                print(f"\n[ERROR] Stopped at: {title}")
                return False

        output_cfg = self.config.get("ps4_build_config", {}).get("output", {})
        pkg_name = Path(output_cfg.get("pkg_name", "FPKGi_v2.0.0_PS4.zip")).with_suffix(".pkg").name
        output_file = pkg_name

        print("\n" + "=" * 60)
        print("OK Build completed successfully")
        print(f"[PKG] Output: {self.build_dir / output_file}")
        print("=" * 60 + "\n")
        print("WARNING️  Note: This output is an actual PS4 .pkg file created by a valid packaging tool.")
        return True


def main():
    project_root = Path(sys.argv[1]) if len(sys.argv) > 1 else Path.cwd()
    builder = PS4PackageBuilder(project_root)
    success = builder.build()
    sys.exit(0 if success else 1)


if __name__ == "__main__":
    main()
