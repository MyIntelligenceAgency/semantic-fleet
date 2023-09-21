# Installing Oobabooga and Configuring Multi-Start Scripts

## Overview

This guide covers how to install Oobabooga, download models, and configure multi-start scripts for running multiple models.

## Setting Up Oobabooga

1. **Determine Your GPU VRAM**: 
  
   First, you need to know your GPU's VRAM to choose models that fit your hardware. Use [GPU-Z](https://www.techpowerup.com/gpuz/) to find this information.

2. **Install Oobabooga**: 

   Oobabooga's Gradio Web UI is great open source Python web application for hosting Large Language Models. 
   
   To set it up:
     - Download the zip file that matches your OS from [Oobabooga GitHub](https://github.com/oobabooga/text-generation-webui).
       - Options include: [Windows](https://github.com/oobabooga/one-click-installers/oobabooga-windows.zip), [Linux](https://github.com/oobabooga/one-click-installers/oobabooga-linux.zip), [macOS](https://github.com/oobabooga/one-click-installers/oobabooga-macos.zip), and [WSL](https://github.com/oobabooga/one-click-installers/oobabooga-wsl.zip).
     - Unzip the file and run "start". This will install and start the Web UI locally. You can access it usually at `http://localhost:7860/`.

## Downloading Models

   Most models can be found on [HuggingFace](https://huggingface.co/). 

   - For a curated list, check out the [open_llm_leaderboard](https://huggingface.co/spaces/HuggingFaceH4/open_llm_leaderboard).
  
   - [Tom Jobbins](https://huggingface.co/TheBloke) offers quantized models that balance memory usage and performance. 

   To download:
  
   1. Go to the **Models** tab in Oobabooga's UI.
   2. Use the "Download custom model or LoRA" function.
   3. For each model you want:
         - Visit its Hugging Face page, like [TheBloke's StableBeluga-13B-GGML](https://huggingface.co/TheBloke/StableBeluga-13B-GGML).
         - Copy the model name.
         - Paste it into Oobabooga's model section, adding `:main`. For example, "TheBloke/StableBeluga-13B-GGML:main".
         - Click **Download**.

  🚨 **Special Note for quantized GGML and GGUF Models**: 
      
   - These models come in different quantizations, only one of which may be used at a given time.
   - You can choose to download a single file, but if you proceed to download the whole directory as with other models, Oobabooga will download all versions and pick the first model file alphabetically, often a 2bit quantized version, which you probably don't want to select.
   - **Tip**: Start the download in Oobabooga to create the required subfolder, then stop it. Download the specific model file you want from Hugging Face and place it in this new subfolder. This way, you can later adjust quantization according to measured performances and available memory by swapping files,  without having to change your settings.

  *For suggestion about which models to download, read next section with default values provided in the multi-start scripts.*

## Configuring Multi-Start Scripts

1. **Locate Multi-Start Scripts**: You'll find the script for your environment in the [MultistartScripts directory](../oobabooga/MultistartScripts).
2. **Copy and Customize**: Copy this script to your Oobabooga 1-click-install directory. Modify it to match the models you've installed locally. If you haven't chosen models yet, the script has default suggestions based on different GPU capabilities. Note that each model instance needs three ports: one for the Web UI, one for the blocking API, and one for the websockets-based streaming API.
3. **Run the Multi-Start Script**: Execute the `.bat` or `.sh` script to start the models you want to run. If you're using WSL, you'll need administrator access for port forwarding.

## Using Oobabooga Models with Semantic-Kernel

- For examples of how to use Oobabooga models with semantic-kernel, check out the [oobabooga notebooks](../dotnet/notebooks/README.md).
- For concurrent multi-model access, see the [oobabooga chat notebook](../dotnet/notebooks/02-oobabooga-chat-capabilities.ipynb).
- To learn about model routing capabilities, refer to the [MultiConnector Integration Test Guide](../dotnet/src/IntegrationTests/Connectors/MultiConnector/README.md).