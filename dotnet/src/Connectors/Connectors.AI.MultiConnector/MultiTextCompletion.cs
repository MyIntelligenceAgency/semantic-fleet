// Copyright (c) MyIA. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.TextGeneration;
using MyIA.SemanticKernel.Connectors.AI.MultiConnector.Analysis;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector;

/// <summary>
/// Represents a text completion comprising several child completion connectors and capable of routing completion calls to specific connectors.
/// Offers analysis capabilities where a primary completion connector is tasked with vetting secondary connectors.
/// </summary>
public class MultiTextCompletion : ITextGenerationService
{
    private readonly ILogger? _logger;
    private readonly IReadOnlyList<NamedTextCompletion> _textCompletions;
    private readonly MultiTextCompletionSettings _settings;
    private readonly Channel<ConnectorTest> _dataCollectionChannel;

    /// <summary>
    /// Initializes a new instance of the MultiTextCompletion class.
    /// </summary>
    /// <param name="settings">An instance of the <see cref="MultiTextCompletionSettings"/> to configure the multi Text completion.</param>
    /// <param name="mainTextCompletion">The primary text completion to used by default for completion calls and vetting other completion providers.</param>
    /// <param name="analysisTaskCancellationToken">The cancellation token to use for the completion manager.</param>
    /// <param name="loggerFactory">An optional logger for instrumentation.</param>
    /// <param name="otherCompletions">The secondary text completions that need vetting to be used for completion calls.</param>
    public MultiTextCompletion(MultiTextCompletionSettings settings,
        NamedTextCompletion mainTextCompletion,
        CancellationToken? analysisTaskCancellationToken,
        ILoggerFactory? loggerFactory = null,
        params NamedTextCompletion[]? otherCompletions)
    {
        this._settings = settings;
        this._logger = loggerFactory is not null ? loggerFactory.CreateLogger(this.GetType()) : NullLogger.Instance;
        this._textCompletions = new[] { mainTextCompletion }.Concat(otherCompletions ?? Array.Empty<NamedTextCompletion>()).ToArray();
        this._dataCollectionChannel = Channel.CreateUnbounded<ConnectorTest>();

        this.StartManagementTask(analysisTaskCancellationToken ?? CancellationToken.None);
    }

    /// <summary>
    /// The list of text completions that are part of this multi-completion
    /// </summary>
    public IReadOnlyList<NamedTextCompletion> TextCompletions => this._textCompletions;

    /// <summary>
    /// The settings used to configure this multi-completion
    /// </summary>
    public MultiTextCompletionSettings Settings => this._settings;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> Attributes { get; } = new Dictionary<string, object?>();

    /// <inheritdoc />
    public async Task<IReadOnlyList<TextContent>> GetTextContentsAsync(string text, PromptExecutionSettings? requestSettings, Kernel? kernel, CancellationToken cancellationToken)
    {
        this._logger?.LogTrace("\n## Starting MultiTextCompletion.GetTextContentsAsync\n");
        var completionJob = new CompletionJob(text, requestSettings);
        var session = this._settings.GetMultiCompletionSession(completionJob, this.TextCompletions, this._logger);
        this._logger?.LogTrace("Calling chosen completion with adjusted prompt and settings");

        // Child completions are already materialized in SK 1.78 (TextContent.Text), so the
        // legacy ITextResult laziness collapses: we capture the first result now and wrap it in
        // the session's AsyncLazy producer to keep the costing/logging/analysis path unchanged.
        var completions = await session.NamedTextCompletion.TextCompletion.GetCompletionsAsync(session.CallJob.Prompt, session.CallJob.RequestSettings, cancellationToken).ConfigureAwait(false);
        session.Stopwatch.Stop();

        var firstResult = completions.Count > 0 ? (completions[0] ?? string.Empty) : string.Empty;

        var resultLazy = new AsyncLazy<string>(() => Task.FromResult(firstResult), cancellationToken);

        session.ResultProducer = resultLazy;

        await this.ProcessTextCompletionResultsAsync(session, cancellationToken).ConfigureAwait(false);

        this._logger?.LogTrace("\n## Ending MultiTextCompletion.GetTextContentsAsync\n");
        return completions.Select(c => new TextContent(c, modelId: null)).ToList();
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<StreamingTextContent> GetStreamingTextContentsAsync(string text, PromptExecutionSettings? requestSettings, Kernel? kernel, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        this._logger?.LogTrace("\n## Starting MultiTextCompletion.GetStreamingTextContentsAsync\n");
        var completionJob = new CompletionJob(text, requestSettings);
        var session = this._settings.GetMultiCompletionSession(completionJob, this.TextCompletions, this._logger);
        this._logger?.LogTrace("Calling chosen completion with adjusted prompt and settings");
        var result = session.NamedTextCompletion.TextCompletion.GetStreamingTextContentsAsync(session.CallJob.Prompt, session.CallJob.RequestSettings, kernel, cancellationToken);

        // The child stream is single-enumerable; tee it once into a buffer while accumulating the
        // full text for the session producer, then replay the buffer to the caller. This preserves
        // the legacy semantics where the costing/logging/analysis task and the caller both observed
        // the (single) result stream.
        var sb = new StringBuilder();
        var buffered = new List<StreamingTextContent>();
        await foreach (var chunk in result.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            sb.Append(chunk.Text);
            buffered.Add(chunk);
        }

        session.Stopwatch.Stop();

        var resultLazy = new AsyncLazy<string>(() => Task.FromResult(sb.ToString()), cancellationToken);

        session.ResultProducer = resultLazy;

        // Await costing/analysis rather than fire-and-forgetting it. The buffer is already
        // fully materialized above (the child stream was consumed into `buffered`/`sb`), so
        // awaiting here is safe (no deadlock, no data race) and makes the streaming path
        // consistent with the non-streaming GetTextContentsAsync (which awaits the same call).
        // Previously this was un-awaited, which meant: creditor cost was debited on a race
        // (non-deterministic for cost assertions), and any exception in costing/analysis was
        // swallowed as an unobserved task exception.
        await this.ProcessTextCompletionResultsAsync(session, cancellationToken).ConfigureAwait(false);

        this._logger?.LogTrace("\n## Ending MultiTextCompletion.GetStreamingTextContentsAsync\n");

        foreach (var chunk in buffered)
        {
            yield return chunk;
        }
    }

    /// <summary>
    /// This method ends the multi-completion session and collects the results for analysis if needed
    /// </summary>
    private async Task ProcessTextCompletionResultsAsync(MultiCompletionSession session, CancellationToken cancellationToken)
    {
        var costDebited = await this.ApplyCreditorCostsAsync(session.CallJob.Prompt, session.ResultProducer, session.NamedTextCompletion).ConfigureAwait(false);

        if (this._settings.EnablePromptSampling && session.PromptSettings.IsSampleNeeded(session))
        {
            session.PromptSettings.AddSessionPrompt(session.InputJob.Prompt);
            await this.CollectResultForTestAsync(session, costDebited, cancellationToken).ConfigureAwait(false);
        }

        if (this._settings.LogCallResult)
        {
            var connectorName = session.NamedTextCompletion.Name;
            var duration = session.Stopwatch.Elapsed;
            var callPromptText = session.CallJob.Prompt;
            var result = await session.ResultProducer.Value.ConfigureAwait(false);
            this._logger?.LogInformation("\n\nMULTI-COMPLETION returned for connector: {0}: duration:{1} \nADJUSTED PROMPT:\n{2}\n\nRESULT:\n{3}\n\n",
                connectorName,
                duration,
                this._settings.GeneratePromptLog(callPromptText),
                this._settings.GeneratePromptLog(result));
        }
    }

    /// <summary>
    /// This method applies the cost of the text completion (input + result) to the creditor if one is configured
    /// </summary>
    private async Task<decimal> ApplyCreditorCostsAsync(string text, AsyncLazy<string> resultLazy, NamedTextCompletion textCompletion)
    {
        decimal cost = 0;
        if (this._settings.Creditor != null)
        {
            var result = await resultLazy.Value.ConfigureAwait(false);
            cost = textCompletion.GetCost(text, result);
            this._settings.Creditor.Credit(cost);
        }

        return cost;
    }

    /// <summary>
    /// Asynchronously collects results from a prompt call to evaluate connectors against the same prompt.
    /// </summary>
    private async Task CollectResultForTestAsync(MultiCompletionSession session, decimal textCompletionCost, CancellationToken cancellationToken)
    {
        var result = await session.ResultProducer.Value.ConfigureAwait(false);

        var duration = session.Stopwatch.Elapsed;

        // For the management task
        ConnectorTest connectorTest = ConnectorTest.Create(session.InputJob, session.NamedTextCompletion, result, duration, textCompletionCost);
        this.AppendConnectorTest(connectorTest);
    }

    /// <summary>
    /// Starts a management task charged with collecting and analyzing prompt connector usage.
    /// </summary>
    private void StartManagementTask(CancellationToken cancellationToken)
    {
        Task.Factory.StartNew(
            async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await this.CollectSamplesAsync(cancellationToken).ConfigureAwait(false);
                }
            },
            cancellationToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    /// <summary>
    /// Asynchronously receives new ConnectorTest from completion calls, evaluate available connectors against tests and perform analysis to vet connectors.
    /// </summary>
    private async Task CollectSamplesAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (await this._dataCollectionChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var testSeries = new List<ConnectorTest>();

                while (this._dataCollectionChannel.Reader.TryRead(out var newSample))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        var now = DateTime.Now;
                        var delay = newSample.Timestamp + this._settings.SampleCollectionDelay - now;

                        if (delay > TimeSpan.FromMilliseconds(1))
                        {
                            this._logger?.LogTrace(message: "CollectSamplesAsync adding collection delay {0}", delay);
                            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                        }

                        testSeries.Add(newSample);
                    }
                }

                this._logger?.LogTrace(message: "CollectSamplesAsync collected a new ConnectorTest series to analyze", testSeries);

                var analysisJob = new AnalysisJob(this._settings, this._textCompletions, this._logger, cancellationToken);
                // Save the tests
                var needTest = this._settings.AnalysisSettings.SaveSamplesReturnTestsNeeded(testSeries, analysisJob);

                if (needTest)
                {
                    // Once you have a batch ready, write it to the channel
                    await this._settings.AnalysisSettings.AddAnalysisJobAsync(analysisJob).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException exception)
        {
            this._logger?.LogTrace("CollectSamplesAsync task was cancelled with exception {0}", exception, exception.ToString());
        }
        catch (Exception exception)
        {
            var message = "CollectSamplesAsync task failed";
            this._logger?.LogError("{0} with exception {1}", exception, message, exception.ToString());
            throw new KernelException(message, exception);
        }
    }

    /// <summary>
    /// Appends a connector test to the test channel listened to in the Optimization long running task.
    /// </summary>
    private void AppendConnectorTest(ConnectorTest connectorTest)
    {
        if (this._settings.LogTestCollection)
        {
            this._logger?.LogDebug("Collecting new original sample to test with duration {0},\nORIGINAL_PROMPT:\n{1}\nORIGINAL_RESULT:\n{2}", connectorTest.Duration,
                this._settings.GeneratePromptLog(connectorTest.Prompt),
                this._settings.GeneratePromptLog(connectorTest.Result));
        }

        this._dataCollectionChannel.Writer.TryWrite(connectorTest);
    }
}
