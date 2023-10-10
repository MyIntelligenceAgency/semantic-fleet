# Semantic Kernel C# Notebooks with Oobabooga Connector

## Setup

- [Install .NET 7](https://dotnet.microsoft.com/download/dotnet/7.0)
- [Install Visual Studio Code (VS Code)](https://code.visualstudio.com)
- [Install the "Polyglot" extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-interactive-vscode)

## Configuration

Run the `00-AI-settings.ipynb` notebook to set up your AI backend configurations. This will guide you through setting up OpenAI and Oobabooga and will save your settings to `config/settings.json`.

Alternatively, you can copy and rename `config/settings.example.json` to `config/settings.json` and manually edit the file to set up your AI backend configurations.

## Topics

0. [AI Backend Settings](00-AI-settings.ipynb)
1. [Text Completion with Oobabooga](01-oobabooga-text-completion.ipynb)
2. [Chat Capabilities with Oobabooga](02-oobabooga-chat-capabilities.ipynb)
3. [Introduction to Multiconnector with Arithmetic mocks](03-multiConnector-intro-with-arithmetic-mocks.ipynb)
4. [Offloading a semantic function from a large LLM to a smaller one with a multiconnector](04-multiConnector-optimization-with-summarization-prompt.ipynb)