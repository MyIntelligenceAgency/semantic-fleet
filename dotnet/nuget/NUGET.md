# About Semantic Fleet

**Semantic Fleet** extends the capabilities of Semantic Kernel (SK) by offering a fleet of specialized connectors. With Semantic Fleet, you can easily integrate various AI services into your SK-powered applications, all managed by a superior Large Language Model (LLM) as your fleet captain.

## Initial Features

### 🌟 Oobabooga Connector

Our first release, the Oobabooga Connector, is designed to make text and chat completion a breeze. Whether you need real-time updates or simple blocking API calls, this connector has got you covered.

- **Text Blocking & Streaming**: Use the `OobaboogaTextCompletion` class for both blocking and streaming text completion.
- **Chat Blocking & Streaming**: The `OobaboogaChatCompletion` class handles chat-based completion with both blocking and streaming APIs.
- **Advanced Settings**: Customize your requests with a rich set of parameters for fine-grained control.

### 🚀 Multiconnector

The Multiconnector builds on the Oobabooga Connector by allowing a superior LLM (like ChatGPT) to manage a series of smaller local LLMs hosted in Oobabooga. It routes prompts according to their types and the models' vetting on sampled data.

- **Prompt Routing**: Automatically routes prompts to the most suitable LLM.
- **Sampled Data Vetting**: Uses superior LLM to vet and optimize the use of various connectors.

## Getting Started ⚡

- **Documentation**: For a deep dive into how to use Semantic Fleet and its features, check out our [GitHub repository](https://github.com/MyIntelligenceAgency/semantic-fleet).
