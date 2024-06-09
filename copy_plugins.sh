#!/bin/bash
# How to run: bash copy_plugins.sh

# Define the source and destination directories
declare -A dirs=(
    ["./NovelsCollector.Plugins/Source.DTruyenCom/bin/Debug/net8.0/"]="./NovelsCollector.Core/bin/Debug/net8.0/source-plugins/DTruyenCom"
    ["./NovelsCollector.Plugins/Source.SSTruyenVn/bin/Debug/net8.0/"]="./NovelsCollector.Core/bin/Debug/net8.0/source-plugins/SSTruyenVn"
    ["./NovelsCollector.Plugins/Source.TruyenFullVn/bin/Debug/net8.0/"]="./NovelsCollector.Core/bin/Debug/net8.0/source-plugins/TruyenFullVn"
    ["./NovelsCollector.Plugins/Source.TruyenTangThuVienVn/bin/Debug/net8.0/"]="./NovelsCollector.Core/bin/Debug/net8.0/source-plugins/TruyenTangThuVienVn"
	["./NovelsCollector.Plugins/Exporter.SimpleEPub/bin/Debug/net8.0/"]="./NovelsCollector.Core/bin/Debug/net8.0/exporter-plugins/SimpleEPub"
	["./NovelsCollector.Plugins/Exporter.SimplePDF/bin/Debug/net8.0/"]="./NovelsCollector.Core/bin/Debug/net8.0/exporter-plugins/SimplePDF"
	["./NovelsCollector.Plugins/Exporter.SimpleMobi/bin/Debug/net8.0/"]="./NovelsCollector.Core/bin/Debug/net8.0/exporter-plugins/SimpleMobi"
)

# Loop through each source and destination pair
for src in "${!dirs[@]}"; do
    dest=${dirs[$src]}
    
    # Create the destination directory if it does not exist
    mkdir -p "$dest"
    
    # Copy all files from the source to the destination
    cp -r "$src"* "$dest"
    
    # Print a message indicating the operation was successful
    echo "Copied files from $src to $dest"
done