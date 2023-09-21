#!/bin/bash

# Define your models here...
declare -a INSTANCE_DATA=(
# Orca Mini 3B q4 GGML: 4 GB
# "7860,5000,5010,--model TheBloke_orca_mini_3B-GGML --loader llama.cpp --monkey-patch --xformers --n-gpu-layers 200000"
# Red Pajama 3B : 6.2 GB
# "7861,5001,5011,--model togethercomputer_RedPajama-INCITE-Chat-3B-v1 --loader transformers --monkey-patch --xformers --n-gpu-layers 200000"
# Stable Beluga 7B q4 GGML : 6.2 GB
# "7862,5002,5012,--model TheBloke_StableBeluga-7B-GGML --loader llama.cpp --monkey-patch --xformers --n-gpu-layers 200000"
# Stable Beluga 13B q4 GGML : 10.3 GB
"7863,5003,5013,--model TheBloke_StableBeluga-13B-GGML --loader llama.cpp --monkey-patch --xformers --n-gpu-layers 200000"
# Upstage Llama instruct 30B q4 GGML : 22 GB
# "7864,5004,5014,--model TheBloke_upstage-llama-30b-instruct-2048-GGML --loader llama.cpp --monkey-patch --xformers --n-gpu-layers 200000"
)

# Loop through instances
for currentInstance in "${INSTANCE_DATA[@]}"; do
    IFS=',' read -ra ADDR <<< "$currentInstance"
    
    LISTEN_PORT=${ADDR[0]}
    API_BLOCKING_PORT=${ADDR[1]}
    API_STREAMING_PORT=${ADDR[2]}
    STATIC_FLAGS=${ADDR[3]}
    
    export OOBABOOGA_FLAGS="--listen --api --verbose $STATIC_FLAGS --listen-host 0.0.0.0 --listen-port $LISTEN_PORT --api-blocking-port $API_BLOCKING_PORT --api-streaming-port $API_STREAMING_PORT"
    
    echo "About to launch with:"
    echo "OOBABOOGA_FLAGS: $OOBABOOGA_FLAGS"
    
    # Start the main script in a new process
    ./start_macos.sh &

    unset OOBABOOGA_FLAGS
done
