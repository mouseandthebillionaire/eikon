#!/usr/bin/env python3
"""
Rename layer files from frame_000000.png format to 00.png through 30.png format
"""

import os
import glob

def rename_layer_files(base_dir):
    """
    Rename all files in layer directories from frame_XXXXXX.png to XX.png format
    
    Args:
        base_dir (str): Base directory containing layer_0 through layer_5 folders
    """
    # Process each layer directory
    for layer_num in range(6):
        layer_dir = os.path.join(base_dir, f"layer_{layer_num}")
        
        if not os.path.exists(layer_dir):
            print(f"Warning: {layer_dir} does not exist, skipping...")
            continue
            
        print(f"Processing {layer_dir}...")
        
        # Get all PNG files in the layer directory
        png_files = glob.glob(os.path.join(layer_dir, "frame_*.png"))
        png_files.sort()  # Sort to ensure consistent ordering
        
        renamed_count = 0
        
        for old_path in png_files:
            filename = os.path.basename(old_path)
            
            # Extract frame number from frame_000000.png format
            if filename.startswith("frame_") and filename.endswith(".png"):
                # Get the 6-digit number part
                frame_number_str = filename[6:12]  # Extract "000000" part
                try:
                    frame_number = int(frame_number_str)
                    # Convert to 2-digit format (00, 01, 02, etc.)
                    new_filename = f"{frame_number:02d}.png"
                    new_path = os.path.join(layer_dir, new_filename)
                    
                    # Rename the file
                    os.rename(old_path, new_path)
                    renamed_count += 1
                    print(f"  Renamed {filename} -> {new_filename}")
                    
                except ValueError:
                    print(f"  Warning: Could not parse frame number from {filename}")
            else:
                print(f"  Warning: Unexpected filename format: {filename}")
        
        print(f"  Renamed {renamed_count} files in {layer_dir}")

def main():
    base_dir = "../Assets/Resources/paperWaterSprites"
    
    print("Starting layer file renaming...")
    print(f"Base directory: {base_dir}")
    
    if not os.path.exists(base_dir):
        print(f"Error: Base directory '{base_dir}' does not exist!")
        return
    
    rename_layer_files(base_dir)
    
    print("Layer file renaming complete!")

if __name__ == "__main__":
    main()
