// Copyright (c) MyIA. All rights reserved.

using System.Collections.Generic;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.Configuration;

/// <summary>
/// Represents the main configuration part for a MultiConnector made of Oobabooga secondary connectors.
/// </summary>
public class MultiOobaboogaConnectorConfiguration
{
    /// <summary>
    /// Default endpoint for secondary Oobabooga connectors. Can be overriden by each connector.
    /// </summary>
    public string OobaboogaEndPoint { get; set; } = "http://localhost";

    /// <summary>
    /// Global parameters applied for all connector transforms.
    /// </summary>
    public Dictionary<string, string> GlobalParameters { get; set; } = new();

    /// <summary>
    /// Names of secondary connectors to include in the MultiConnector, among those configured.
    /// </summary>
    public List<string> IncludedConnectors { get; set; } = new();

    /// <summary>
    /// Overrides to <see cref="IncludedConnectors"/>, meant for development purposes using a second "xxx.development.json" settings file.
    /// </summary>
    public List<string> IncludedConnectorsDev { get; set; } = new();

    /// <summary>
    /// List of Oobabooga connectors available to include as secondary connectors in the MultiConnector.
    /// </summary>
    public List<OobaboogaConnectorConfiguration> OobaboogaCompletions { get; set; } = new();
}
