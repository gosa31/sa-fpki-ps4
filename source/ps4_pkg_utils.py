# -*- coding: utf-8 -*-
#!/usr/bin/env python3
import os
import subprocess
from pathlib import Path

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