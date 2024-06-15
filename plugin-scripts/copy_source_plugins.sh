#!/bin/bash
# How to run: bash copy_source_plugins.sh

# Define building mode: Debug or Release
build_mode="Debug"

# Define the base paths for source and destination directories
src_base="../BE.NovelsCollector/NovelsCollector.Plugins/Sources"
dest_base="../BE.NovelsCollector/NovelsCollector.WebAPI/bin/$build_mode/net8.0/Plugins/Sources"

# Define the source and destination directories
declare -A dirs=(
    ["$src_base/Source.DTruyenCom/bin/$build_mode/net8.0/"]="$dest_base/DTruyenCom"
    ["$src_base/Source.SSTruyenVn/bin/$build_mode/net8.0/"]="$dest_base/SSTruyenVn"
    ["$src_base/Source.TruyenFullVn/bin/$build_mode/net8.0/"]="$dest_base/TruyenFullVn"
    ["$src_base/Source.TruyenTangThuVienVn/bin/$build_mode/net8.0/"]="$dest_base/TruyenTangThuVienVn"
)

# Loop through each source and destination pair
for src in "${!dirs[@]}"; do
    dest=${dirs[$src]}
    
    # Delete the destination directory if it exists
    rm -rf "$dest"
    
    # Create the destination directory
    mkdir -p "$dest"
    
    # Copy all files from the source to the destination
    cp -r "$src"* "$dest"
	
	# Delete all .pdb files in the destination directory
    find "$dest" -name "*.pdb" -type f -delete
    
    # Print a message indicating the operation was successful
    echo "Copied files from $src to $dest"
done
