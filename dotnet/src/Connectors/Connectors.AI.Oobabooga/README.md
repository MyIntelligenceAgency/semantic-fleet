# Oobabooga Connector

## Introduction
The Oobabooga Connector is a powerful tool for interacting with the Oobabooga API. It provides a .NET interface for both blocking and streaming completion and chat APIs. This document will guide you through the various settings and usage patterns.

## Installation
```bash
# Installation steps here
```

## Usage

### Four Main Cases

1. **Text Blocking**: For simple text-based completion, use the `OobaboogaTextCompletion` class with blocking API.
2. **Text Streaming**: For text-based completion with real-time updates, use the `OobaboogaTextCompletion` class with streaming API.
3. **Chat Blocking**: For chat-based completion, use the `OobaboogaChatCompletion` class with blocking API
3. **Chat streaming**: For chat-based completion, with real-time streaming use the `OobaboogaChatCompletion` class with streaming API.

### Kernel extensions

You can use the `Kernel.Builder` class to configure your semantic kernel. Below are examples of how to add a text and chat completion service using the Oobabooga connector.

#### Using Oobabooga for Text Completion

```csharp
var kernelWithTextCompletion = Kernel.Builder
    .WithLoggerFactory(loggerFactory)
    .WithOobaboogaTextCompletionService(
        new OobaboogaTextCompletionSettings(/* your settings here */),
        "OobaboogaTextServiceId",  // Optional: Local identifier for the AI service
        true                       // Optional: Set as default service
    )
    .Build();
```

#### Using Oobabooga for Chat Completion

```csharp
var kernelWithChatCompletion = Kernel.Builder
    .WithLoggerFactory(loggerFactory)
    .WithOobaboogaChatCompletionService(
        new OobaboogaChatCompletionSettings(/* your settings here */),
        "OobaboogaChatServiceId",  // Optional: Local identifier for the AI service
        true                       // Optional: Set as default service
    )
    .Build();
```

### Low-level usage

Normally , you would use the kernel extensions above to add the Oobabooga connector to your semantic kernel. However, you can also use the connector directly. Here's how:

#### Text Blocking
For blocking text completion, you can use the `OobaboogaTextCompletion` class. Here's a quick example:

```csharp
var settings = new OobaboogaTextCompletionSettings(endpoint: new Uri("http://localhost/"), blockingPort: 1234);
var textCompletion = new OobaboogaTextCompletion(settings);
var result = await textCompletion.GetCompletionsAsync("Hello, world!", new CompleteRequestSettings());
```

#### Text Streaming
For streaming text completion, you can use the `CompleteStreamAsync` method. Here's how:

```csharp
var settings = new OobaboogaTextCompletionSettings(endpoint: new Uri("http://localhost/"), streamingPort: 2345);
var textCompletion = new OobaboogaTextCompletion(settings);
var results = textCompletion.CompleteStreamAsync("Hello, world!", new CompleteRequestSettings());

await foreach (var result in results)
{
    Console.WriteLine(result);
}
```

#### Chat Blocking
For blocking chat completion, you can use the `OobaboogaChatCompletion` class. Here's a quick example:

```csharp
var settings = new OobaboogaChatCompletionSettings(endpoint: new Uri("http://localhost/"), blockingPort: 3456);
var chatCompletion = new OobaboogaChatCompletion(settings);
var result = await chatCompletion.GetCompletionsAsync(new List<Message> { new Message { Role = "user", Content = "Hello!" } }, new CompleteRequestSettings());
```

#### Chat Streaming
For streaming chat completion, you can use the `GetStreamingChatCompletionsAsync` method. Here's how:

```csharp
var settings = new OobaboogaChatCompletionSettings(endpoint: new Uri("http://localhost/"), streamingPort: 4567);
var chatCompletion = new OobaboogaChatCompletion(settings);
var results = chatCompletion.GetStreamingChatCompletionsAsync(new List<Message> { new Message { Role = "user", Content = "Hello!" } }, new CompleteRequestSettings());

await foreach (var result in results)
{
    Console.WriteLine(result.Content);
}
```



## Additional Settings

### General Settings

These settings are common to both `OobaboogaTextCompletionSettings` and `OobaboogaChatCompletionSettings`:

- **Endpoint**: The service API endpoint to which requests should be sent.
- **BlockingPort**: The port used for handling blocking requests.
- **StreamingPort**: The port used for handling streaming requests.

### Advanced Settings

- **ConcurrentSemaphore**: Optional. You can set a hard limit on the max number of concurrent calls by providing a `SemaphoreSlim`. Calls in excess will wait for existing consumers to release the semaphore.
  
- **UseWebSocketsPooling**: Determines whether the connector should use WebSocket pooling to reuse WebSockets and prevent resource exhaustion in case of high load.

- **WebSocketsCleanUpCancellationToken**: Optional. If WebSocket pooling is enabled, you can provide a `CancellationToken` to properly dispose of the clean-up tasks when disposing of the connector.

- **KeepAliveWebSocketsDuration**: Specifies the time (in milliseconds) to keep pooled WebSockets in the pool before flushing them.

- **WebSocketFactory**: The factory used to create WebSockets for making streaming API requests. This is especially useful if you want to customize the WebSocket options.

- **HttpClient**: Optional. The HTTP client used for making blocking API requests. If not specified, a default client will be used.

- **LoggerFactory**: Optional. Application logger for debugging and monitoring.

- **WebSocketBufferSize**: Controls the size of the buffer used to receive WebSocket packets.

### Custom Completion Parameters

The `OobaboogaCompletionSettings<TParameters>` class serves as the base class for both `OobaboogaTextCompletionSettings` and  `OobaboogaChatCompletionSettings`. 

In order to customize completion, the 2 following properties are available:

- **OverrideRequestSettings**: Determines whether or not to use the overlapping Semantic Kernel request settings for the completion request.

- **OobaboogaParameters**: Gets or sets the parameters controlling the specific completion request parameters, which differ between standard text completion and chat completion: TParameters type corresponds here to OobaboogaCompletionParameters and OobaboogaChatCompletionParameters classes respectively.

OobaboogaCompletionParameters and OobaboogaChatCompletionParameters classes offer a rich set of parameters for text and chat completion. Here's a deep dive into how these parameters work and interact.

#### Overlapping Fields with SK

Those parameters supplied by semantic kernel with completion calls are either passed on or overriden.

- **MaxNewTokens**: Similar to SK's `MaxTokens`, controls the maximum number of tokens to generate.
- **Temperature**: Modulates the randomness of the next token probabilities.
- **TopP**: Similar to SK's `TopP`, controls the cumulative probability for token selection.

#### Unique Oobabooga Parameters for Text and Chat Completion

- **Preset**: Named presets for generation parameters. Check out the [default Oobabooga presets](https://github.com/oobabooga/text-generation-webui/tree/main/presets).
- **DoSample**: Decides whether to use sampling or greedy decoding.
- **TypicalP**: Measures the similarity of conditional probabilities between target and random tokens.
- **EpsilonCutoff**: Sets a probability floor for token sampling.
- **Tfs**: Controls Tail Free Sampling.
- **TopA**: Implements Top A Sampling based on token importance.
- ... (and many more, each with its specific role and default value).


#### Unique Oobabooga Parameters for Chat Completion only

- **Mode**: The mode of chat completion. Valid options: 'chat', 'chat-instruct', 'instruct'.
- **Character**: The character name for the chat completion.
- **InstructionTemplate**: The instruction template for instruct mode chat completion.
- **YourName**: The name to use for the user in the chat completion.
- **Regenerate**: Determines whether to regenerate the chat completion.
- **Continue**: Determines whether to continue the chat completion.
- **StopAtNewline**: Determines whether to stop at newline in the chat completion.
- **ChatGenerationAttempts**: The number of chat generation attempts.
- **ChatInstructCommand**: The chat-instruct command for the chat completion when corresponding mode is used.
- **ContextInstruct**: The instruction context for the chat-instruct / instruct completion.

## Additional References

For more in-depth understanding and testing, you can refer to the following:

- **Unit Tests**: Check out the actual test file [here](../../Connectors.UnitTests/Oobabooga/Completion/OobaboogaCompletionTests.cs).
- **Integration Tests**: Dive into the integration tests [here](../../IntegrationTests/Connectors/Oobabooga/OobaboogaCompletionTests.cs).
- **Future Notebooks**: Keep an eye out for Jupyter Notebooks that will provide interactive examples and use-cases. These will be added to the [notebooks directory](../../notebooks/) in the future.