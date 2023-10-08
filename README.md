# Semantic-Fleet 🚀

[![Oobabooga Connector Nuget package](https://img.shields.io/nuget/vpre/MyIA.SemanticKernel.Connectors.AI.Oobabooga?label=nuget%20Oobabooga%20Connector)](https://www.nuget.org/packages/MyIA.SemanticKernel.Connectors.AI.Oobabooga/)
[![Multiconnector Nuget package](https://img.shields.io/nuget/vpre/MyIA.SemanticKernel.Connectors.AI.MultiConnector?label=nuget%20MultiConnector)](https://www.nuget.org/packages/MyIA.SemanticKernel.Connectors.AI.MultiConnector/)

## Overview

Semantic-Fleet is a repository designed to extend the capabilities of [Semantic Kernel](https://github.com/microsoft/semantic-kernel). It focuses on providing connectors to small large language models (e.g. Llamas), and to provide tools for distributing work to a fleet of models, with ChatGPT serving as the captain of the fleet. This repository is more than just a collection of existing connectors; it's a platform for future innovations in the .NET ecosystem for AI.

## What's On Deck?

### 🤖 Oobabooga Connector

A robust connector that currently covers the main Oobabooga's specific blocking and streaming completion and chat APIs. 

📖 **Learn More**: 
- [Installing Oobabooga and Configuring Multi-Start Scripts](./docs/OOBABOOGA.md)
- [Oobabooga Connector Guide](./dotnet/src/Connectors/Connectors.AI.Oobabooga/README.md)
- Don't forget to check-out [notebooks](./dotnet/notebooks/README.md). They provide a great overview of what's possible with our published connectors.

#### Installation

Install the package via NuGet:

```bash
dotnet add package MyIA.SemanticKernel.Connectors.AI.Oobabooga
```

In .Net interactive :

```csharp
#r "nuget: MyIA.SemanticKernel.Connectors.AI.Oobabooga"
```


#### Quick Start

Different settings are used for text and chat completion, both in blocking and streaming modes. Here's a quick example for text completion:

```csharp
var settings = new OobaboogaTextCompletionSettings(endpoint: new Uri("http://localhost/"),  blockingPort: 5000, streamingPort: 5005);
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

- **Open AI API**: Oobabooga offers a dedicated extension mimicking Open AI API. It extends support to embeddings and image generation models. This will be available as a separate package.
- **Probabilistic MultiConnector**: We will be adding some Infer.Net magic to make MultiConnector even smarter. More specifically, the following examples will be merged and integrated into the model vetting process.
   - [Student Skills](https://dotnet.github.io/infer/userguide/Student%20skills.html)
   - [Assessing People's Skills](https://mbmlbook.com/LearningSkills.html)
   - [Difficulty vs Ability](https://dotnet.github.io/infer/userguide/Difficulty%20versus%20ability.html)
   - [Calibrating reviews](https://dotnet.github.io/infer/userguide/Calibrating%20reviews%20of%20conference%20submissions.html)  
- **Spark.Net Integration**: Get ready to host a cluster of mini local LLMs.

## NuGet Packages 

Here is the [Nuget Package for Oobabooga connector](https://www.nuget.org/packages/MyIA.SemanticKernel.Connectors.AI.Oobabooga/)

We'll be providing NuGet packages for both the Oobabooga Connector and MultiConnector for easier integration into your projects.

## 🤝 Contributing

Got something to add? We'd love to see it. Check out our [contributing guidelines](./CONTRIBUTING.md).

Got something you'd like to get added? Do you want those future features already? We'd love you to [get in touch](https://github.com/MyIntelligenceAgency) !
