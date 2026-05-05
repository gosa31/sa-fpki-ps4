# -*- coding: utf-8 -*-
#!/usr/bin/env python3
"""
FPKGi Real PS4 PKG Builder
Creates real PS4 packages using open-source tools and proper PKG structure.
This uses the official PS4 PKG format via proper toolchain integration.
"""

import json
import os
import shutil
import sys
import hashlib
import zipfile
import subprocess
from pathlib import Path
from datetime import datetime

def write_param_sfo(output_path, sfo_data):
    """Generates a valid binary param.sfo file for PS4."""
    out_path = Path(output_path)
    out_path.parent.mkdir(parents=True, exist_ok=True)
    
    magic = b'\0PSF'
    version = b'\x01\x01\x00\x00'
    keys = sorted(sfo_data.keys())
    
    key_table = b''
    key_offsets = []
    for k in keys:
        key_offsets.append(len(key_table))
        key_table += k.encode('utf-8') + b'\0'
        
    data_table = b''
    data_offsets = []
    data_formats = []
    data_lengths = []
    data_max_lengths = []
    
    int_keys = {'ATTRIBUTE', 'PARENTAL_LEVEL', 'PUBTOOLINFO'}
    
    for k in keys:
        val = sfo_data[k]
        data_offsets.append(len(data_table))
        if k in int_keys:
            data_formats.append(0x0404)
            val_int = int(val)
            val_bytes = val_int.to_bytes(4, byteorder='little')
            data_table += val_bytes
            data_lengths.append(4)
            data_max_lengths.append(4)
        else:
            data_formats.append(0x0204)
            val_bytes = str(val).encode('utf-8') + b'\0'
            data_table += val_bytes
            data_lengths.append(len(val_bytes))
            data_max_lengths.append(len(val_bytes))
            
    num_entries = len(keys)
    header_size = 20
    entries_size = 16 * num_entries
    key_table_offset = header_size + entries_size
    
    padding = (4 - (len(key_table) % 4)) % 4
    key_table += b'\0' * padding
    
    data_table_offset = key_table_offset + len(key_table)
    
    with open(output_path, 'wb') as f:
        f.write(magic)
        f.write(version)
        f.write(key_table_offset.to_bytes(4, 'little'))
        f.write(data_table_offset.to_bytes(4, 'little'))
        f.write(num_entries.to_bytes(4, 'little'))
        for i in range(num_entries):
            f.write(key_offsets[i].to_bytes(2, 'little'))
            f.write(data_formats[i].to_bytes(2, 'little'))
            f.write(data_lengths[i].to_bytes(4, 'little'))
            f.write(data_max_lengths[i].to_bytes(4, 'little'))
            f.write(data_offsets[i].to_bytes(4, 'little'))
        f.write(key_table)
        f.write(data_table)
        
def create_pkg_with_tool(builds_dir, pkg_output):
    """Calls orbis-pub-cmd to build the actual .pkg file."""
    print("[*] Attempting to create PS4 PKG file...")
    try:
        result = subprocess.run([
            "orbis-pub-cmd.exe", "img_create",
            "--oformat", "pkg",
            str(builds_dir),
            str(pkg_output)
        ], capture_output=True, text=True, timeout=60)
        
        if result.returncode == 0:
            print(f"[OK] Created PKG file: {pkg_output}")
            return True
        print(f"[!] orbis-pub-cmd failed: {result.stderr}")
    except FileNotFoundError:
        print("[!] orbis-pub-cmd.exe not found.")
    except Exception as exc:
        print(f"[!] PKG creation failed: {exc}")
        
    return False


def create_param_sfo(output_path, app_id="FPKG00001", title="FPKGi v2.0.0"):
    """Create a minimal but valid param.sfo file."""
    print(f"[*] Creating param.sfo for {app_id}...")
    sfo_data = {
        "APP_TYPE": "Game",
        "ATTRIBUTE": "0",
        "CATEGORY": "gm",
        "CONTENT_ID": f"UP0001-{app_id}_00",
        "PARENTAL_LEVEL": "3",
        "PUBTOOLINFO": "100",
        "TITLE": title,
        "TITLE_ID": app_id,
        "VERSION": "02.00",
        "SYSTEM_VER": "05.050",
    }
    write_param_sfo(output_path, sfo_data)
    print("[OK] Created param.sfo")


def create_real_pkg(project_root, build_name="FPKGi_v2.0.0_PS4"):
    """Create a real, valid PS4 PKG archive."""
    print("\n" + "=" * 60)
    print("  FPKGi Real PS4 PKG Creator")
    print("=" * 60 + "\n")
    
    project_root = Path(project_root)
    builds_dir = project_root / "Builds" / "PS4"
    pkg_output = project_root / "build" / f"{build_name}.pkg"
    
    # Step 1: Create necessary files
    builds_dir.mkdir(parents=True, exist_ok=True)
    (builds_dir / "sce_sys").mkdir(exist_ok=True)
    (builds_dir / "app").mkdir(exist_ok=True)
    
    # Create param.sfo
    create_param_sfo(builds_dir / "sce_sys" / "param.sfo")
    
    # Check for eboot.bin
    eboot_path = builds_dir / "app" / "eboot.bin"
    if not eboot_path.exists() or eboot_path.stat().st_size == 0:
        print("[ERROR] Missing or invalid eboot.bin. Please build the PS4 executable first using Unity or PS4 SDK.")
        print(f"Expected at: {eboot_path}")
        return False
    
    # Step 2: Create PKG structure
    print("[*] Building PS4 package...")
    pkg_output.parent.mkdir(parents=True, exist_ok=True)

    if create_pkg_with_tool(builds_dir, pkg_output):
        sha256_file = pkg_output.with_suffix('.sha256')
        sha256 = hashlib.sha256()
        with open(pkg_output, 'rb') as f:
            for chunk in iter(lambda: f.read(4096), b''):
                sha256.update(chunk)
        with open(sha256_file, 'w') as f:
            f.write(f"{sha256.hexdigest()}  {pkg_output.name}\n")
        print(f"[OK] Created SHA256: {sha256_file}")
    else:
        print("[ERROR] No PS4 packaging tools found. Install orbis-pub-cmd.exe or ps4-pkg-tool.exe to create real PKGs.")
        return False

    print(f"[OK] Created PKG: {pkg_output}")

    pkg_size = pkg_output.stat().st_size / (1024 * 1024)
    print("\n" + "=" * 60)
    print(f"OK Real PS4 PKG created successfully")
    print(f"[PKG] File: {pkg_output.name}")
    print(f"[SIZE] Size: {pkg_size:.2f} MB")
    print(f"[SHA256] SHA256: {sha256.hexdigest()[:16]}...")
    print("=" * 60 + "\n")
    
    return True


if __name__ == "__main__":
    project_root = Path(sys.argv[1]) if len(sys.argv) > 1 else Path.cwd()
    try:
        success = create_real_pkg(project_root)
        sys.exit(0 if success else 1)
    except Exception as e:
        print(f"[ERROR] Error: {e}")
        sys.exit(1)
