# Semantic-Fleet 🚀

[![Nuget package](https://img.shields.io/nuget/vpre/MyIA.SemanticKernel.Connectors.AI.Oobabooga)](https://www.nuget.org/packages/MyIA.SemanticKernel.Connectors.AI.Oobabooga/)

## Overview

Semantic-Fleet is a dedicated repository designed to extend the capabilities of Semantic Kernel. It focuses on providing open Low-Level Model (LLM) connectors, with ChatGPT serving as the captain of the fleet. This repository is more than just a collection of existing connectors; it's a platform for future innovations in the .NET ecosystem for AI.

## What's On Deck?

### 🤖 Oobabooga Connector

A robust connector that currently covers the main non-OpenAI specific blocking and streaming completion and chat APIs. 

📖 **Learn More**: [Oobabooga Connector Guide](./dotnet/src/Connectors/Connectors.AI.Oobabooga/README.md)


#### Installation

Install the package via NuGet (Coming Soon):

```bash
dotnet add package MyIA.SemanticKernel.Connectors.AI.Oobabooga --version 0.33.2
```

In .Net interactive :

```csharp
#r "nuget: MyIA.SemanticKernel.Connectors.AI.Oobabooga, 0.33.2"
```


#### Quick Start

Different settings are used for text and chat completion, both in blocking and streaming modes. Here's a quick example for text completion:

```csharp
var settings = new OobaboogaTextCompletionSettings(endpoint: new Uri("http://localhost/"), streamingPort: 8080);
var oobabooga = new OobaboogaTextCompletion(settings);

// Get text completions
var completions = await oobabooga.GetCompletionsAsync("Hello, world!", new CompleteRequestSettings());
```



### 🌐 MultiConnector
 
Why stick to one when you can have many? MultiConnector lets you integrate multiple LLMs seamlessly.

📖 **Learn More**: [MultiConnector Guide](./dotnet/src/IntegrationTests/Connectors/MultiConnector/README.md)


## 📚 Notebooks

Want a overview of what's possible with our published connectors? 
Our .Net interactive notebooks are a great place to start.

📖 **Learn More**: [Notebooks Guide](./dotnet/notebooks/README.md)

## Future Directions

- **Probabilistic MultiConnector**: We will be adding some Infer.Net magic to make MultiConnector even smarter.
- **Spark.Net Integration**: Get ready to host a cluster of mini local LLMs.

## NuGet Packages (Coming Soon)

Here is the [Nuget Package for Oobabooga connector](https://www.nuget.org/packages/MyIA.SemanticKernel.Connectors.AI.Oobabooga/)

We'll be providing NuGet packages for both the Oobabooga Connector and MultiConnector for easier integration into your projects.

## Contributing

We welcome contributions that align with the project's vision. Please refer to the contributing guidelines for more details.
