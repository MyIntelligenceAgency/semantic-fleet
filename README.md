# Semantic-Fleet 🚀

## Overview

Semantic-Fleet is a dedicated repository designed to extend the capabilities of Semantic Kernel. It focuses on providing open Low-Level Model (LLM) connectors, with ChatGPT serving as the captain of the fleet. This repository is more than just a collection of existing connectors; it's a platform for future innovations in the .NET ecosystem for AI.

## What It Does Now

### Oobabooga Connector

A robust connector that currently covers the main non-OpenAI specific blocking and streaming completion and chat APIs. 

For more details, check out the [Oobabooga Connector README](./dotnet/src/Connectors/Connectors.AI.Oobabooga/README.md).

#### Installation

Install the package via NuGet (Coming Soon):

```bash
dotnet add package OobaboogaConnector
```

#### Usage

Different settings are used for text and chat completion, both in blocking and streaming modes. Here's a quick example for text completion:

```csharp
var settings = new OobaboogaTextCompletionSettings(endpoint: new Uri("http://localhost/"), streamingPort: 8080);
var oobabooga = new OobaboogaTextCompletion(settings);

// Get text completions
var completions = await oobabooga.GetCompletionsAsync("Hello, world!", new CompleteRequestSettings());
```



### MultiConnector

An advanced connector that allows for seamless integration with multiple LLMs. 

## Future Directions

- **Probabilistic MultiConnector**: Plans to integrate Infer.Net to make the MultiConnector probabilistic, opening the way for a mixture of experts approach.
- **Spark.Net Integration**: An upcoming feature to extend the fleet with capabilities for hosting a cluster of small Oobabooga models.

## NuGet Packages (Coming Soon)

We'll be providing NuGet packages for both the Oobabooga Connector and MultiConnector for easier integration into your projects.

## Contributing

We welcome contributions that align with the project's vision. Please refer to the contributing guidelines for more details.
