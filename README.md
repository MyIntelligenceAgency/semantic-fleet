# Semantic-Fleet 🚀

[![Multiconnector Nuget package](https://img.shields.io/nuget/vpre/MyIA.SemanticKernel.Connectors.AI.MultiConnector?label=nuget%20MultiConnector)](https://www.nuget.org/packages/MyIA.SemanticKernel.Connectors.AI.MultiConnector/)

## Overview

Semantic-Fleet is a repository designed to extend the capabilities of [Semantic Kernel](https://github.com/microsoft/semantic-kernel). It focuses on providing connectors to small large language models (e.g. Llamas), and to provide tools for distributing work to a fleet of models, with ChatGPT serving as the captain of the fleet. This repository is more than just a collection of existing connectors; it's a platform for future innovations in the .NET ecosystem for AI.

### Retired: the Oobabooga websocket transport

The dedicated Oobabooga websocket connector has been removed: upstream Oobabooga replaced its websocket API with an OpenAI-compatible HTTP API, so the bridge no longer had anything to talk to. Secondary connectors are now configured through `MultiOpenAICompatibleConnectorConfiguration`, which targets any OpenAI-compatible endpoint (vLLM, LM Studio, llama.cpp server, OpenRouter...). The 50k-chunk load bench that used to run over the websocket transport is preserved on the OpenAI-compatible transport ([PR #101](https://github.com/MyIntelligenceAgency/semantic-fleet/pull/101)). Historical setup notes remain in [docs/OOBABOOGA.md](./docs/OOBABOOGA.md).

## What's On Deck?

### 🌐 MultiConnector
 
Why stick to one when you can have many? MultiConnector lets you integrate multiple LLMs seamlessly, optimizing for speed and cost. It intelligently offloads tasks from a primary, more expensive connector to a secondary, more cost-effective one without sacrificing reliability nor performance.

📖 **Learn More**: [MultiConnector Guide](./dotnet/src/IntegrationTests/Connectors/MultiConnector/README.md)

#### Installation

Install the package via NuGet:

```bash
dotnet add package MyIA.SemanticKernel.Connectors.AI.MultiConnector
```

In .Net interactive :

```csharp
#r "nuget: MyIA.SemanticKernel.Connectors.AI.MultiConnector
```

#### Quick Start

Multiconnector has many settings controlling how to route text completion calls, and how to autmatically sample completions from a primary connector, test, evaluate and update the routing settings to use secondary connectors.

```csharp
var settings = new MultiTextCompletionSettings();

// (...) Creating a primary openAiNamedCompletion and secondaryCompletions

var builder = Microsoft.SemanticKernel.Kernel.Builder;

builder.WithMultiConnectorCompletionService(
    serviceId: null,
    settings: settings,
    mainTextCompletion: openAiNamedCompletion,
    setAsDefault: true,
    analysisTaskCancellationToken: cleanupToken.Token,
    otherCompletions: secondaryCompletions.ToArray());

var kernel = builder.Build();

// Get text completion from primary connector first
 var result = await kernel.RunAsync(semanticFunctionOrPlan, contextVariables, cancellationToken: cleanupToken.Token).ConfigureAwait(false);

 // (...) Peform analysis manually or automatically depending on settings

// Get text completion from secondary connectors
 var optimizedResult = await kernel.RunAsync(semanticFunctionOrPlan, contextVariables, cancellationToken: cleanupToken.Token).ConfigureAwait(false);

```

For a detailed overview of how to fill the gaps, please refer to the notebooks and integration tests.


## 📚 Notebooks

Want a overview of what's possible with our published connectors? 
Our .Net interactive notebooks are a great place to start.

📖 **Learn More**: [Notebooks Guide](./dotnet/notebooks/README.md)

## Future Directions

- **Probabilistic MultiConnector**: We will be adding some Infer.Net magic to make MultiConnector even smarter. More specifically, the following examples will be merged and integrated into the model vetting process.
   - [Student Skills](https://dotnet.github.io/infer/userguide/Student%20skills.html)
   - [Assessing People's Skills](https://mbmlbook.com/LearningSkills.html)
   - [Difficulty vs Ability](https://dotnet.github.io/infer/userguide/Difficulty%20versus%20ability.html)
   - [Calibrating reviews](https://dotnet.github.io/infer/userguide/Calibrating%20reviews%20of%20conference%20submissions.html)  
- **Spark.Net Integration**: Get ready to host a cluster of mini local LLMs.

## NuGet Packages 

The MultiConnector is published as a NuGet package for easier integration into your projects.

Here is the [Nuget Package for Multiconnector](https://www.nuget.org/packages/MyIA.SemanticKernel.Connectors.AI.Multiconnector/)


## 🤝 Contributing

Got something to add? We'd love to see it. Check out our [contributing guidelines](./CONTRIBUTING.md).

Got something you'd like to get added? Do you want those future features already? We'd love you to [get in touch](https://github.com/MyIntelligenceAgency) !
