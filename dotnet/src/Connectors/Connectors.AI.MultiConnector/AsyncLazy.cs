// Copyright (c) MyIA. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector;

/// <summary>
/// Provides a way to lazily initialize asynchronous tasks.
/// </summary>
/// <typeparam name="T">The type of object that is being asynchronously initialized.</typeparam>
public sealed class AsyncLazy<T> : Lazy<Task<T>>
{
    /// <summary>
    /// Initializes a new instance of the AsyncLazy class that uses the default constructor of T.
    /// </summary>
    /// <param name="value">The value to be returned by the Value property.</param>
    public AsyncLazy(T value)
        : base(() => Task.FromResult(value))
    {
    }

    /// <summary>
    /// Initializes a new instance of the AsyncLazy class with a synchronous value factory.
    /// </summary>
    /// <param name="valueFactory">The value factory.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    public AsyncLazy(Func<T> valueFactory, CancellationToken cancellationToken)
        : base(() => Task.Factory.StartNew<T>(valueFactory, cancellationToken, TaskCreationOptions.None, TaskScheduler.Current))
    {
    }

    /// <summary>
    /// Initializes a new instance of the AsyncLazy class with an asynchronous value factory.
    /// </summary>
    /// <param name="taskFactory">The asynchronous value factory.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    public AsyncLazy(Func<Task<T>> taskFactory, CancellationToken cancellationToken)
        : base(() => Task.Factory.StartNew(taskFactory, cancellationToken, TaskCreationOptions.None, TaskScheduler.Current).Unwrap())
    {
    }
}
