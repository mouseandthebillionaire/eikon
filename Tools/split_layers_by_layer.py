from PIL import Image
import numpy as np
import random
import os
import glob

def split_into_layers(input_path, output_base_dir, cell_size=1):
    """
    Split an image into 6 layers and save them to layer-specific directories.
    
    Args:
        input_path (str): Path to the input image
        output_base_dir (str): Base directory for output folders
        cell_size (int): Size of cells for processing (default: 1)
    """
    # Get the filename without extension for naming
    filename = os.path.basename(input_path)
    name_without_ext = os.path.splitext(filename)[0]
    
    # Open the image
    img = Image.open(input_path)
    
    # Convert to RGBA if not already
    img = img.convert('RGBA')
    
    # Convert to numpy array for easier manipulation
    img_array = np.array(img)
    
    # Create 6 empty layers with same dimensions and RGBA
    layers = [np.zeros_like(img_array) for _ in range(6)]
    
    # Get height and width
    height, width = img_array.shape[:2]
    
    # For each cell in the image
    for y in range(0, height, cell_size):
        for x in range(0, width, cell_size):
            # Get the cell's pixels
            cell_pixels = img_array[y:min(y+cell_size, height), x:min(x+cell_size, width)]
            
            # Check if any pixel in the cell is not fully transparent
            if np.any(cell_pixels[:, :, 3] > 0):
                # Randomly choose a layer (0-5 for 6 layers)
                layer_index = random.randint(0, 5)
                # Place all pixels in the cell to the chosen layer
                layers[layer_index][y:min(y+cell_size, height), x:min(x+cell_size, width)] = cell_pixels
    
    # Save each layer to its respective directory
    for i, layer in enumerate(layers):
        # Convert numpy array back to PIL Image
        layer_img = Image.fromarray(layer)
        # Save with transparency to the layer-specific directory
        layer_dir = os.path.join(output_base_dir, f"layer_{i}")
        os.makedirs(layer_dir, exist_ok=True)
        layer_img.save(os.path.join(layer_dir, filename))

def batch_process_frames(input_dir, output_base_dir, cell_size=1):
    """
    Process all frame images in the input directory and create split layers for each.
    
    Args:
        input_dir (str): Directory containing the input frame images
        output_base_dir (str): Base directory for output folders
        cell_size (int): Size of cells for processing
    """
    # Create base output directory
    os.makedirs(output_base_dir, exist_ok=True)
    
    # Get all PNG files in the input directory
    frame_files = glob.glob(os.path.join(input_dir, "*.png"))
    frame_files.sort()  # Sort to process in order
    
    print(f"Found {len(frame_files)} frame files to process")
    
    for frame_path in frame_files:
        filename = os.path.basename(frame_path)
        print(f"Processing {filename}...")
        
        # Split the image into layers
        split_into_layers(frame_path, output_base_dir, cell_size)
        
        print(f"  Created 6 layers for {filename}")

# Main execution
if __name__ == "__main__":
    print("Starting batch frame processing...")
    
    # Process frames from extracted_frames directory
    input_dir = "extracted_frames"
    output_dir = "../Assets/Resources/paperWaterSprites"
    
    print(f"Processing frames from: {input_dir}")
    print(f"Output directory: {output_dir}")
    
    batch_process_frames(input_dir, output_dir, cell_size=1)
    
    print("Batch processing complete!")
