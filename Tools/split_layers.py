from PIL import Image
import numpy as np
import random

def split_into_layers(input_path, output_prefix, cell_size=1):
    # Open the image
    img = Image.open(input_path)
    
    # Convert to RGBA if not already
    img = img.convert('RGBA')
    
    # Convert to numpy array for easier manipulation
    img_array = np.array(img)
    
    # Create X empty layers with same dimensions and RGBA
    layers = [np.zeros_like(img_array) for _ in range(3)]
    
    # Get height and width
    height, width = img_array.shape[:2]
    
    # For each cell in the image
    for y in range(0, height, cell_size):
        for x in range(0, width, cell_size):
            # Get the cell's pixels
            cell_pixels = img_array[y:min(y+cell_size, height), x:min(x+cell_size, width)]
            
            # Check if any pixel in the cell is not fully transparent
            if np.any(cell_pixels[:, :, 3] > 0):
                # Randomly choose a layer (0-14)
                layer_index = random.randint(0, 2)
                # Place all pixels in the cell to the chosen layer
                layers[layer_index][y:min(y+cell_size, height), x:min(x+cell_size, width)] = cell_pixels
    
    # Save each layer
    for i, layer in enumerate(layers):
        # Convert numpy array back to PIL Image
        layer_img = Image.fromarray(layer)
        # Save with transparency
        layer_img.save(f"{i}.png")  # Just using the layer number as filename

# Example usage
if __name__ == "__main__":
    input_file = "main.png"  # Replace with your input file
    output_prefix = ""  # Replace with your desired output prefix
    cell_size = 10  # Adjust this value to change the cell size (e.g., 2 for 2x2 pixels, 3 for 3x3 pixels)
    split_into_layers(input_file, output_prefix, cell_size) 
