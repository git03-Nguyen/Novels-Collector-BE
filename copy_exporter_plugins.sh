#!/bin/bash
# How to run: bash copy_exporter_plugins.sh

# Define building mode: Debug or Release
build_mode="Debug"

# Define the base paths for source and destination directories
src_base="./NovelsCollector.Plugins/Exporters"
dest_base="./NovelsCollector.Core/bin/$build_mode/net8.0"

# Define the source and destination directories
declare -A dirs=(
    ["$src_base/Exporter.SimpleEPub/bin/$build_mode/net8.0/"]="$dest_base/exporter-plugins/SimpleEPub"
    ["$src_base/Exporter.SimplePDF/bin/$build_mode/net8.0/"]="$dest_base/exporter-plugins/SimplePDF"
    ["$src_base/Exporter.SimpleMobi/bin/$build_mode/net8.0/"]="$dest_base/exporter-plugins/SimpleMobi"
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
    
    # Print a message indicating the operation was successful
    echo "Copied files from $src to $dest"
done
