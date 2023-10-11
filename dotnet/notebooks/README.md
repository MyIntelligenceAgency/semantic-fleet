# Semantic Fleet C# Notebooks

Welcome to the Semantic Fleet C# Notebooks! This collection of notebooks serves as a practical guide to understanding and implementing the features offered by Semantic Fleet, including connectors to local LLMs with Oobabooga and the new MultiConnector for optimizing performance. Whether you're new to Semantic Fleet or looking to dive deeper into its capabilities, these notebooks offer hands-on examples to get you up and running quickly.

Here's what you'll learn:

- **Setting up AI Backends**: How to configure OpenAI and Oobabooga as your AI backends.
- **Text and Chat Completions with Oobabooga**: Dive into text and chat capabilities using the Oobabooga connector.
- **Introduction to MultiConnector**: Learn the basics of using MultiConnector with simple arithmetic mocks.
- **Advanced MultiConnector Optimization**: Explore how to offload tasks from a larger language model to a smaller one, optimizing for speed and cost.

Ready to get started? Follow the setup and configuration steps below!

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
