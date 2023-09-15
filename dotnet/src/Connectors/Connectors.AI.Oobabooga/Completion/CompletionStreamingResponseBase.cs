// Copyright (c) MyIA. All rights reserved.

using System.Text.Json.Serialization;

namespace MyIA.SemanticKernel.Connectors.AI.Oobabooga.Completion;

/// <summary>
/// This class is used to deserialize the result of a streaming completion or chat request to the Oobabooga API.
/// </summary>
public class CompletionStreamingResponseBase
{
    /// <summary>
    /// Constant string representing the event that is fired when text is received from a websocket.
    /// </summary>
    public const string ResponseObjectTextStreamEvent = "text_stream";

    /// <summary>
    /// Constant string representing the event that is fired when streaming from a websocket ends.
    /// </summary>
    public const string ResponseObjectStreamEndEvent = "stream_end";

    /// <summary>
    /// A field used by Oobabooga to signal the type of websocket message sent, e.g. "text_stream" or "stream_end".
    /// </summary>
    [JsonPropertyName("event")]
    public string Event { get; set; } = string.Empty;

    /// <summary>
    /// A field used by Oobabooga to signal the number of messages sent, starting with 0 and incremented on each message.
    /// </summary>
    [JsonPropertyName("message_num")]
    public int MessageNum { get; set; }
}
