// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.Analysis;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.PromptSettings;

/// <summary>
/// Represents the settings for multiple connectors associated with a particular type of prompt.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay}")]
public class PromptMultiConnectorSettings
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => $"{this.PromptType.PromptName} - {this.ConnectorSettingsDictionary.Count} connector settings";

    /// <summary>
    /// Gets or sets the type of prompt associated with these settings.
    /// </summary>
    public PromptType PromptType { get; set; } = new();

    /// <summary>
    /// Choose whether to apply the model specific transforms for this prompt type
    /// </summary>
    public bool ApplyModelTransform { get; set; } = true;

    /// <summary>
    /// Optionally transform the input prompt specifically
    /// </summary>
    public PromptTransform? PromptTypeTransform { get; set; }

    /// <summary>
    /// Gets a dictionary mapping connector names to their associated settings for this prompt type.
    /// </summary>
    public ConcurrentDictionary<string, PromptConnectorSettings> ConnectorSettingsDictionary { get; } = new();

    /// <summary>
    /// Retrieves the settings associated with a specific connector for the prompt type.
    /// </summary>
    /// <param name="connectorName">The name of the connector.</param>
    /// <returns>The <see cref="PromptConnectorSettings"/> associated with the given connector name.</returns>
    public PromptConnectorSettings GetConnectorSettings(string connectorName)
    {
        if (!this.ConnectorSettingsDictionary.TryGetValue(connectorName, out var promptConnectorSettings))
        {
            promptConnectorSettings = new PromptConnectorSettings();
            this.ConnectorSettingsDictionary[connectorName] = promptConnectorSettings;
        }

        return promptConnectorSettings;
    }

    /// <summary>
    /// Selects the appropriate text completion to use based on the vetting evaluations analyzed.
    /// </summary>
    /// <param name="completionJob">The prompt and request settings to find the appropriate completion for</param>
    /// <param name="namedTextCompletions">The list of available text completions.</param>
    /// <param name="settingsConnectorComparer"></param>
    /// <returns>The selected <see cref="NamedTextCompletion"/>.</returns>
    public (NamedTextCompletion namedTextCompletion, PromptConnectorSettings promptConnectorSettings) SelectAppropriateTextCompletion(CompletionJob completionJob, IReadOnlyList<NamedTextCompletion> namedTextCompletions, Func<CompletionJob, PromptConnectorSettings, PromptConnectorSettings, int> settingsConnectorComparer)
    {
        var filteredConnectors = new List<(NamedTextCompletion textCompletion, PromptConnectorSettings promptConnectorSettings)>();
        filteredConnectors.Add((namedTextCompletions[0], this.GetConnectorSettings(namedTextCompletions[0].Name)));
        foreach (var namedTextCompletion in namedTextCompletions.Skip(1))
        {
            var promptConnectorSettings = this.GetConnectorSettings(namedTextCompletion.Name);
            if (promptConnectorSettings.VettingLevel > 0)
            {
                filteredConnectors.Add((namedTextCompletion, promptConnectorSettings));
            }
        }

        if (filteredConnectors.Count > 1)
        {
            filteredConnectors.Sort((c1, c2) => settingsConnectorComparer(completionJob, c1.promptConnectorSettings, c2.promptConnectorSettings));
        }

        return filteredConnectors[0];
    }

    /// <summary>
    /// Adds a prompt to the current session.
    /// </summary>
    /// <param name="prompt">The prompt to add.</param>
    public void AddSessionPrompt(string prompt)
    {
        this._currentSessionPrompts[prompt] = true;
    }

    private readonly ConcurrentDictionary<string, bool> _currentSessionPrompts = new();

    /// <summary>
    /// Determines if a sample is needed based on the current session.
    /// </summary>
    /// <param name="session">The current multi-completion session.</param>
    /// <returns>True if a sample is needed, otherwise false.</returns>
    public bool IsSampleNeeded(MultiCompletionSession session)
    {
        var toReturn = (session.IsNewPrompt
                        || (this.PromptType.Instances.Count < session.MultiConnectorSettings.MaxInstanceNb
                            && !this.PromptType.Instances.Contains(session.InputJob.Prompt)))
                       && !this._currentSessionPrompts.ContainsKey(session.InputJob.Prompt)
                       && (session.MultiConnectorSettings.SampleVettedConnectors || session.AvailableCompletions.Any(namedTextCompletion =>
                           !this.ConnectorSettingsDictionary.TryGetValue(namedTextCompletion.Name, out PromptConnectorSettings? value)
                           || value?.VettingLevel == 0));
        return toReturn;
    }

    /// <summary>
    /// Retrieves the completions to test based on the original test and available completions.
    /// </summary>
    /// <param name="originalTest">The original connector test.</param>
    /// <param name="namedTextCompletions">The list of available text completions.</param>
    /// <param name="enablePrimaryCompletionTests">Flag to enable primary completion tests.</param>
    /// <returns>An enumerable of <see cref="NamedTextCompletion"/> to test.</returns>
    public IEnumerable<NamedTextCompletion> GetCompletionsToTest(ConnectorTest originalTest, IReadOnlyList<NamedTextCompletion> namedTextCompletions, bool enablePrimaryCompletionTests)
    {
        return namedTextCompletions.Where(
            namedTextCompletion => (namedTextCompletion.Name != originalTest.ConnectorName || enablePrimaryCompletionTests)
                                   && (!this.ConnectorSettingsDictionary.TryGetValue(namedTextCompletion.Name, out PromptConnectorSettings value)
                                       || value.VettingLevel == 0));
    }
}
