# -*- coding: utf-8 -*-
#!/usr/bin/env python3
"""
FPKGi PS4 Package Converter
Convert a built Unity PS4 folder into a package archive structure.
"""

import json
import sys
import shutil
import hashlib
import subprocess
from pathlib import Path
from datetime import datetime

from ps4_pkg_utils import write_param_sfo


class PS4PackageConverter:
    def __init__(self, build_dir, output_name="FPKGi_v2.0.0_PS4"):
        self.build_dir = Path(build_dir)
        self.output_name = output_name
        self.pkg_dir = self.build_dir / "pkg_contents"
        self.metadata = self._load_metadata()
        self.config = self._load_config()
        self.executable_names = ["eboot.bin", "FPKGi.elf"]

    def _load_metadata(self):
        metadata_file = self.build_dir / "ps4_metadata.json"
        if metadata_file.exists():
            with open(metadata_file, encoding="utf-8") as f:
                return json.load(f)
        return {
            "app_info": {
                "title": "FPKGi v2.0.0",
                "title_id": "FPKG00001",
                "version": "02.00",
            }
        }

    def _load_config(self):
        config_file = self.build_dir / "ps4_build_config.json"
        if config_file.exists():
            with open(config_file, encoding="utf-8") as f:
                return json.load(f)
        return {}

    def prepare_package_structure(self):
        print("[*] Preparing package structure...")
        self.pkg_dir.mkdir(parents=True, exist_ok=True)
        (self.pkg_dir / "sce_sys").mkdir(parents=True, exist_ok=True)
        (self.pkg_dir / "app").mkdir(parents=True, exist_ok=True)
        print("[OK] Package folders created")
        return True

    def copy_executable(self):
        print("[*] Finding PS4 executable in build folder...")
        found = False
        for name in self.executable_names:
            candidate = self.build_dir / name
            if candidate.exists() and candidate.stat().st_size > 0:
                shutil.copy2(candidate, self.pkg_dir / "app" / "eboot.bin")
                print(f"[OK] Copied {candidate.name}")
                found = True
                break
            elif candidate.exists():
                print(f"[ERROR] Invalid executable (size 0): {candidate}")
        if not found:
            print("[ERROR] No PS4 executable found in build folder")
            print("    Expected one of: eboot.bin, FPKGi.elf")
            return False
        return True

    def copy_assets(self):
        print("[*] Copying build assets...")
        directories = self.config.get("ps4_build_config", {}).get("directories", [
            {"src": "Assets/Resources", "dst": "app/assets"},
            {"src": "Assets/Fonts", "dst": "app/fonts"},
            {"src": "Assets/Scenes", "dst": "app/scenes"},
        ])
        copied_any = False
        for entry in directories:
            src = self.build_dir / entry.get("src", "")
            dst = self.pkg_dir / Path(entry.get("dst", ""))
            if src.exists():
                dst.mkdir(parents=True, exist_ok=True)
                shutil.copytree(src, dst, dirs_exist_ok=True)
                print(f"[OK] Copied {entry.get('src')} -> {entry.get('dst')}")
                copied_any = True
            else:
                print(f"[!] Skipped missing source: {src}")
        if not copied_any:
            print("[!] Warning: No directories were copied. Check your build layout.")
        return True

    def create_sfo_file(self):
        print("[*] Writing PS4 metadata file...")
        app_info = self.metadata.get("app_info", {})
        sfo_data = {
            "APP_TYPE": "Game",
            "ATTRIBUTE": "0",
            "CATEGORY": "gm",
            "CONTENT_ID": f"UP0001-{app_info.get('title_id', 'FPKG00001')}_00",
            "PARENTAL_LEVEL": "3",
            "PUBTOOLINFO": "100",
            "TITLE": app_info.get("title", "FPKGi"),
            "TITLE_ID": app_info.get("title_id", "FPKG00001"),
            "VERSION": app_info.get("version", "02.00"),
            "SYSTEM_VER": "05.050",
        }
        write_param_sfo(self.pkg_dir / "sce_sys" / "param.sfo", sfo_data)
        with open(self.pkg_dir / "sce_sys" / "param.sfo.json", "w", encoding="utf-8") as f:
            json.dump(sfo_data, f, indent=2, ensure_ascii=False)
        with open(self.pkg_dir / "sce_sys" / "param.sfo.txt", "w", encoding="utf-8") as f:
            for key, value in sfo_data.items():
                f.write(f"{key}={value}\n")
        print("[OK] Binary param.sfo created")
        return True

    def copy_icon_files(self):
        print("[*] Copying icon files if available...")
        icons = self.config.get("ps4_build_config", {}).get("icons", {})
        copied = False
        for icon_name, icon_src in icons.items():
            src_path = self.build_dir / icon_src
            if src_path.exists():
                shutil.copy2(src_path, self.pkg_dir / "sce_sys" / Path(icon_src).name)
                print(f"[OK] Copied {icon_src}")
                copied = True
            else:
                print(f"[!] Missing icon file: {src_path}")
        if not copied:
            print("[!] No icon files copied. Provide icon0.png/bg0.png/pic0.png in build folder.")
        return True

    def create_package_info(self):
        print("[*] Creating package description file...")
        package_info = {
            "build_date": datetime.now().isoformat(),
            "app_info": self.metadata.get("app_info", {}),
            "instructions": [
                "Copy this folder to a USB drive formatted as exFAT",
                "Connect the USB to the Jailbroken PS4",
                "Go to Settings > System Software > Install Package Files",
                "Select the folder from USB and install",
            ],
        }
        with open(self.pkg_dir / "PACKAGEINFO.json", "w", encoding="utf-8") as f:
            json.dump(package_info, f, indent=2, ensure_ascii=False)
        print("[OK] Package info file created")
        return True

    def create_installation_guide(self):
        print("[*] Creating installation guide...")
        guide = f"""# FPKGi PS4 Installation Guide

## Requirements
- Jailbroken or developer PS4
- USB drive formatted as exFAT
- PS4 binary built for this project

## Steps
1. Copy this entire extracted folder to the USB drive
2. Connect the USB to the PS4
3. Go to Settings > System Software > Install Package Files
4. Choose the folder and install

## App Info
- Title: {self.metadata.get('app_info', {}).get('title', 'FPKGi v2.0.0')}
- Title ID: {self.metadata.get('app_info', {}).get('title_id', 'FPKG00001')}
- Version: {self.metadata.get('app_info', {}).get('version', '02.00')}

## Notes
This archive is a package folder export. It is not a signed official PS4 .pkg file.
"""
        with open(self.pkg_dir / "INSTALLATION_EN.md", "w", encoding="utf-8") as f:
            f.write(guide)
        print("[OK] Installation guide created")
        return True

    def create_final_package(self):
        pkg_path = self.build_dir / f"{self.output_name}.pkg"
        if not pkg_path.exists():
            print(f"[ERROR] Cannot create archive because PKG file is missing: {pkg_path}")
            return False

        print("[*] Creating archive...")
        output_path = self.build_dir / f"{self.output_name}.zip"
        try:
            shutil.make_archive(str(output_path.with_suffix('')), "zip", self.pkg_dir)
            print(f"[OK] Created archive: {output_path.name}")
            return True
        except Exception as exc:
            print(f"[ERROR] Failed to create archive: {exc}")
            return False

    def create_pkg_file(self):
        print("[*] Attempting to create PS4 PKG file...")
        pkg_output = self.build_dir / f"{self.output_name}.pkg"
        try:
            # Try using orbis-pub-cmd if available
            result = subprocess.run([
                "orbis-pub-cmd.exe", "img_create", 
                "--oformat", "pkg", 
                str(self.pkg_dir), 
                str(pkg_output)
            ], capture_output=True, text=True, timeout=60)
            if result.returncode == 0:
                print(f"[OK] Created PKG file: {pkg_output.name}")
                return True
            else:
                print(f"[!] orbis-pub-cmd failed: {result.stderr}")
        except FileNotFoundError:
            print("[!] orbis-pub-cmd.exe not found. Install Orbis SDK or use alternative tools.")
        except subprocess.TimeoutExpired:
            print("[!] PKG creation timed out.")
        except Exception as exc:
            print(f"[!] PKG creation failed: {exc}")
        
        # Fallback: try ps4-pkg-tool or similar
        try:
            result = subprocess.run([
                "ps4-pkg-tool.exe", "create", 
                str(self.pkg_dir), 
                str(pkg_output)
            ], capture_output=True, text=True, timeout=60)
            if result.returncode == 0:
                print(f"[OK] Created PKG file with ps4-pkg-tool: {pkg_output.name}")
                return True
            else:
                print(f"[!] ps4-pkg-tool failed: {result.stderr}")
        except FileNotFoundError:
            print("[!] ps4-pkg-tool.exe not found.")
        except subprocess.TimeoutExpired:
            print("[!] PKG creation timed out.")
        except Exception as exc:
            print(f"[!] PKG creation failed: {exc}")
        
        print("[!] Could not create PKG file. Ensure Orbis SDK or compatible tools are installed and the package structure is valid.")
        return False  # Changed to True to continue

    def create_checksum(self):
        print("[*] Creating SHA256 checksums...")
        files_to_check = [
            self.build_dir / f"{self.output_name}.zip",
            self.build_dir / f"{self.output_name}.pkg"
        ]
        checksum_file = self.build_dir / f"{self.output_name}.sha256"
        with open(checksum_file, "w", encoding="utf-8") as f:
            for package_file in files_to_check:
                if package_file.exists():
                    sha256_hash = hashlib.sha256()
                    with open(package_file, "rb") as pf:
                        for chunk in iter(lambda: pf.read(4096), b""):
                            sha256_hash.update(chunk)
                    f.write(f"{sha256_hash.hexdigest()}  {package_file.name}\n")
                    print(f"[OK] Checksum for {package_file.name}")
                else:
                    print(f"[!] File not found for checksum: {package_file.name}")
        if any(f.exists() for f in files_to_check):
            print(f"[OK] Created checksum file: {checksum_file.name}")
            return True
        else:
            print("[!] No package files found for checksum")
            return False

    def build(self):
        print("\n" + "=" * 60)
        print("  FPKGi PS4 Package Converter")
        print("=" * 60 + "\n")
        steps = [
            ("Prepare Structure", self.prepare_package_structure),
            ("Copy Executable", self.copy_executable),
            ("Copy Resources", self.copy_assets),
            ("Write SFO Metadata", self.create_sfo_file),
            ("Copy Icons", self.copy_icon_files),
            ("Create Package Info", self.create_package_info),
            ("Create Installation Guide", self.create_installation_guide),
            ("Create PKG File", self.create_pkg_file),
            ("Create Archive", self.create_final_package),
            ("Create Checksum", self.create_checksum),
        ]
        for title, func in steps:
            if not func():
                print(f"\n[ERROR] Stopped at: {title}")
                return False
        print("\n" + "=" * 60)
        print("OK Converter completed successfully")
        print(f"[PKG] Output: {self.build_dir / f'{self.output_name}.zip'}")
        return True


def main():
    build_dir = sys.argv[1] if len(sys.argv) > 1 else "Builds/PS4"
    build_path = Path(build_dir)
    if not build_path.exists():
        print(f"[ERROR] Build folder not found: {build_path}")
        sys.exit(1)
    converter = PS4PackageConverter(build_path)
    success = converter.build()
    sys.exit(0 if success else 1)


if __name__ == "__main__":
    main()
