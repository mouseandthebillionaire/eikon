from PIL import Image
import numpy as np
import random
import os
import glob

def split_into_layers(input_path, output_dir, cell_size=1):
    """
    Split an image into 6 layers and save them to the specified output directory.
    
    Args:
        input_path (str): Path to the input image
        output_dir (str): Directory to save the split layers
        cell_size (int): Size of cells for processing (default: 1)
    """
    # Create output directory if it doesn't exist
    os.makedirs(output_dir, exist_ok=True)
    
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
    
    # Save each layer
    for i, layer in enumerate(layers):
        # Convert numpy array back to PIL Image
        layer_img = Image.fromarray(layer)
        # Save with transparency to the output directory
        layer_img.save(os.path.join(output_dir, f"layer_{i}.png"))

def batch_process_images(input_dir=".", output_base_dir="split_layers", cell_size=1):
    """
    Process all images 00.png through 36.png and create split layers for each.
    
    Args:
        input_dir (str): Directory containing the input images
        output_base_dir (str): Base directory for output folders
        cell_size (int): Size of cells for processing
    """
    # Create base output directory
    os.makedirs(output_base_dir, exist_ok=True)
    
    # Process images 00 through 36
    for i in range(37):
        # Format with zero padding for two digits
        image_name = f"{i:02d}.png"
        input_path = os.path.join(input_dir, image_name)
        
        # Check if the image exists
        if os.path.exists(input_path):
            print(f"Processing {image_name}...")
            
            # Create output directory for this image
            output_dir = os.path.join(output_base_dir, f"{i:02d}")
            
            # Split the image into layers
            split_into_layers(input_path, output_dir, cell_size)
            
            print(f"  Created 6 layers in {output_dir}/")
        else:
            print(f"Warning: {image_name} not found, skipping...")

def cleanup_meta_files(directory="."):
    """Remove .meta files from the specified directory."""
    meta_files = glob.glob(os.path.join(directory, "*.meta"))
    for meta_file in meta_files:
        os.remove(meta_file)
        print(f"Removed {meta_file}")

# Main execution
if __name__ == "__main__":
    print("Starting batch image processing...")
    
    # Clean up .meta files first
    print("Cleaning up .meta files...")
    cleanup_meta_files()
    
    # Process all images
    print("Processing images 00-36...")
    batch_process_images(cell_size=1)
    
    print("Batch processing complete!") 
