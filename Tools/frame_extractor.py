#!/usr/bin/env python3
"""
Frame Extractor for .mov files

This script extracts frames from a .mov video file and saves them as individual images.
Supports various output formats (PNG, JPEG) and allows you to specify frame extraction rate.
"""

import cv2
import os
import argparse
import sys
from pathlib import Path


def extract_frames(video_path, output_dir, frame_interval=1, image_format='png', quality=95):
    """
    Extract frames from a video file.
    
    Args:
        video_path (str): Path to the input video file
        output_dir (str): Directory to save extracted frames
        frame_interval (int): Extract every Nth frame (1 = every frame, 2 = every other frame, etc.)
        image_format (str): Output image format ('png', 'jpg', 'jpeg')
        quality (int): JPEG quality (1-100, only applies to JPEG format)
    """
    
    # Validate input file
    if not os.path.exists(video_path):
        print(f"Error: Video file '{video_path}' not found.")
        return False
    
    # Create output directory if it doesn't exist
    os.makedirs(output_dir, exist_ok=True)
    
    # Open video file
    cap = cv2.VideoCapture(video_path)
    
    if not cap.isOpened():
        print(f"Error: Could not open video file '{video_path}'")
        return False
    
    # Get video properties
    total_frames = int(cap.get(cv2.CAP_PROP_FRAME_COUNT))
    fps = cap.get(cv2.CAP_PROP_FPS)
    duration = total_frames / fps if fps > 0 else 0
    
    print(f"Video Info:")
    print(f"  Total frames: {total_frames}")
    print(f"  FPS: {fps:.2f}")
    print(f"  Duration: {duration:.2f} seconds")
    print(f"  Extracting every {frame_interval} frame(s)")
    print()
    
    frame_count = 0
    extracted_count = 0
    
    # Set up image encoding parameters
    if image_format.lower() in ['jpg', 'jpeg']:
        encode_params = [cv2.IMWRITE_JPEG_QUALITY, quality]
        extension = '.jpg'
    else:
        encode_params = [cv2.IMWRITE_PNG_COMPRESSION, 3]  # PNG compression level
        extension = '.png'
    
    print("Extracting frames...")
    
    while True:
        ret, frame = cap.read()
        
        if not ret:
            break
        
        # Extract frame if it matches the interval
        if frame_count % frame_interval == 0:
            # Generate filename with zero-padded frame number
            frame_filename = f"frame_{extracted_count:06d}{extension}"
            frame_path = os.path.join(output_dir, frame_filename)
            
            # Save frame
            success = cv2.imwrite(frame_path, frame, encode_params)
            
            if success:
                extracted_count += 1
                if extracted_count % 10 == 0:  # Progress update every 10 frames
                    print(f"  Extracted {extracted_count} frames...")
            else:
                print(f"  Warning: Failed to save frame {extracted_count}")
        
        frame_count += 1
    
    cap.release()
    
    print(f"\nExtraction complete!")
    print(f"  Total frames processed: {frame_count}")
    print(f"  Frames extracted: {extracted_count}")
    print(f"  Output directory: {output_dir}")
    
    return True


def main():
    parser = argparse.ArgumentParser(
        description="Extract frames from a .mov video file",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  python frame_extractor.py video.mov
  python frame_extractor.py video.mov -o frames/ -i 5 -f jpg
  python frame_extractor.py video.mov --interval 10 --format png --quality 90
        """
    )
    
    parser.add_argument('video_path', help='Path to the input .mov file')
    parser.add_argument('-o', '--output', default='extracted_frames', 
                       help='Output directory for extracted frames (default: extracted_frames)')
    parser.add_argument('-i', '--interval', type=int, default=1,
                       help='Extract every Nth frame (default: 1, extract all frames)')
    parser.add_argument('-f', '--format', choices=['png', 'jpg', 'jpeg'], default='png',
                       help='Output image format (default: png)')
    parser.add_argument('-q', '--quality', type=int, default=95, choices=range(1, 101),
                       help='JPEG quality 1-100 (default: 95, only applies to JPEG)')
    
    args = parser.parse_args()
    
    # Validate video file extension
    video_ext = Path(args.video_path).suffix.lower()
    if video_ext not in ['.mov', '.mp4', '.avi', '.mkv', '.wmv', '.flv']:
        print(f"Warning: '{video_ext}' is not a common video format. Proceeding anyway...")
    
    # Extract frames
    success = extract_frames(
        video_path=args.video_path,
        output_dir=args.output,
        frame_interval=args.interval,
        image_format=args.format,
        quality=args.quality
    )
    
    if success:
        print("\nFrame extraction completed successfully!")
    else:
        print("\nFrame extraction failed!")
        sys.exit(1)


if __name__ == "__main__":
    main()
