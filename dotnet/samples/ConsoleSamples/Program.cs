// Copyright (c) MyIA. All rights reserved.

using System.Reflection;
using Microsoft.Extensions.Configuration;
using MyIA.SemanticFleet.ConsoleSamples.Reliability;
using MyIA.SemanticFleet.ConsoleSamples.RepoUtils;

namespace MyIA.SemanticFleet.ConsoleSamples;

/// <summary>
/// Class with main entry point of the program.
/// </summary>
public static class Program
{
    /// <summary>
    /// Main entry point of the program.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task Main(string[] args)
    {
        // Load configuration from environment variables or user secrets.
        LoadUserSecrets();

        // Execution canceled if the user presses Ctrl+C.
        using CancellationTokenSource cancellationTokenSource = new();
        CancellationToken cancelToken = cancellationTokenSource.ConsoleCancellationToken();

        string? defaultFilter = null; // Modify to filter examples

        // Check if args[0] is provided
        string? filter = args.Length > 0 ? args[0] : defaultFilter;

        // Run examples based on the filter
        await RunExamplesAsync(filter, cancelToken);
    }

    /// <summary>
    /// Runs examples asynchronously based on a filter.
    /// </summary>
    /// <param name="filter">The filter to apply to the examples. If null or empty, all examples will be run.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private static async Task RunExamplesAsync(string? filter, CancellationToken cancellationToken)
    {
        var examples = (Assembly.GetExecutingAssembly().GetTypes())
            .Where(type => type.Name.StartsWith("Example", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Filter and run examples
        foreach (var example in examples)
        {
            if (string.IsNullOrEmpty(filter) || example.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    Console.WriteLine($"Running {example.Name}...");

                    var method = example.GetMethod("RunAsync");
                    if (method == null)
                    {
                        Console.WriteLine($"Example {example.Name} not found");
                        continue;
                    }

                    bool hasCancellationToken = method.GetParameters().Any(param => param.ParameterType == typeof(CancellationToken));

                    var taskParameters = hasCancellationToken ? new object[] { cancellationToken } : null;
                    if (method.Invoke(null, taskParameters) is Task t)
                    {
                        await t.SafeWaitAsync(cancellationToken);
                    }
                    else
                    {
                        method.Invoke(null, null);
                    }
                }
                catch (ConfigurationNotFoundException ex)
                {
                    Console.WriteLine($"{ex.Message}. Skipping example {example.Name}.");
                }
            }
        }
    }

    private static void LoadUserSecrets()
    {
        IConfigurationRoot configRoot = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddUserSecrets<Env>()
            .Build();
        TestConfiguration.Initialize(configRoot);
    }

    private static CancellationToken ConsoleCancellationToken(this CancellationTokenSource tokenSource)
    {
        Console.CancelKeyPress += (s, e) =>
        {
            Console.WriteLine("Canceling...");
            tokenSource.Cancel();
            e.Cancel = true;
        };

        return tokenSource.Token;
    }

    private static async Task SafeWaitAsync(this Task task,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await task.WaitAsync(cancellationToken);
            Console.WriteLine("== DONE ==");
        }
        catch (ConfigurationNotFoundException ex)
        {
            Console.WriteLine($"{ex.Message}. Skipping example.");
        }

        cancellationToken.ThrowIfCancellationRequested();
    }
}
