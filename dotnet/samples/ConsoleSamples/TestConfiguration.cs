// Copyright (c) MyIA. All rights reserved.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using MyIA.SemanticFleet.ConsoleSamples.Reliability;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.Configuration;

internal sealed class TestConfiguration
{
    private readonly IConfigurationRoot _configRoot;
    private static TestConfiguration? s_instance;

    private TestConfiguration(IConfigurationRoot configRoot)
    {
        this._configRoot = configRoot;
    }

    public static void Initialize(IConfigurationRoot configRoot)
    {
        s_instance = new TestConfiguration(configRoot);
    }

    public static OobaboogaConnectorConfiguration Oobabooga => LoadSection<OobaboogaConnectorConfiguration>();

    public static OpenAIConfiguration OpenAI => LoadSection<OpenAIConfiguration>();

    private static T LoadSection<T>([CallerMemberName] string? caller = null)
    {
        if (s_instance == null)
        {
            throw new InvalidOperationException(
                "TestConfiguration must be initialized with a call to Initialize(IConfigurationRoot) before accessing configuration values.");
        }

        if (string.IsNullOrEmpty(caller))
        {
            throw new ArgumentNullException(nameof(caller));
        }

        return s_instance._configRoot.GetSection(caller).Get<T>() ??
               throw new ConfigurationNotFoundException(section: caller);
    }
}
