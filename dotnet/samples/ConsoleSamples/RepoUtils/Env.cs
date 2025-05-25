// Copyright (c) MyIA. All rights reserved.

using Microsoft.Extensions.Configuration;

namespace MyIA.SemanticFleet.ConsoleSamples.RepoUtils;

#pragma warning disable CA1812 // instantiated by AddUserSecrets
internal sealed class Env
#pragma warning restore CA1812
{
    /// <summary>
    /// Simple helper used to load env vars and secrets like credentials,
    /// to avoid hard coding them in the sample code
    /// </summary>
    /// <param name="name">Secret name / Env var name</param>
    /// <returns>Value found in Secret Manager or Environment Variable</returns>
    internal static string Var(string name)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<Env>()
            .Build();

        var value = configuration[name];
        if (!string.IsNullOrEmpty(value))
        {
            return value;
        }

        value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrEmpty(value))
        {
            throw new ConsoleSamplesException($"Secret / Env var not set: {name}");
        }

        return value;
    }
}

/// <summary>
/// Represents an exception specific to the ConsoleSamples application.
/// </summary>
public class ConsoleSamplesException : Exception
{
    /// <summary>
    /// Initializes a new instance of the ConsoleSamplesException class with no error message.
    /// </summary>
    /// <returns>
    /// A new instance of the ConsoleSamplesException class.
    /// </returns>
    public ConsoleSamplesException() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the ConsoleSamplesException class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public ConsoleSamplesException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the ConsoleSamplesException class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ConsoleSamplesException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
