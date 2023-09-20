#pragma warning disable IDE0073
// Copyright (c) Microsoft. All rights reserved.
#pragma warning restore IDE0073

using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace SemanticKernel.UnitTests;

/// <summary>
/// Redirects output to <see cref="ITestOutputHelper"/>.
/// </summary>
public sealed class RedirectOutput : TextWriter, ILogger, ILoggerFactory
{
    private readonly ITestOutputHelper _output;
    private readonly StringBuilder _logs;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedirectOutput"/> class.
    /// </summary>
    public RedirectOutput(ITestOutputHelper output)
    {
        this._output = output;
        this._logs = new StringBuilder();
    }

    /// <inheritdoc />
    public override Encoding Encoding { get; } = Encoding.UTF8;

    /// <inheritdoc />
    public override void WriteLine(string? value)
    {
        this._output.WriteLine(value);
        this._logs.AppendLine(value);
    }

    /// <inheritdoc />
    IDisposable ILogger.BeginScope<TState>(TState state)
    {
        return null!;
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    /// <summary>
    /// Gets the current logs.
    /// </summary>
    public string GetLogs()
    {
        return this._logs.ToString();
    }

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        this._output?.WriteLine(message);
        this._logs.AppendLine(message);
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => this;

    /// <inheritdoc />
    public void AddProvider(ILoggerProvider provider) => throw new NotSupportedException();
}
