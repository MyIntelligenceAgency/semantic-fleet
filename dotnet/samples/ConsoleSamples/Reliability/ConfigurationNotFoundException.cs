// Copyright (c) MyIA. All rights reserved.

namespace MyIA.SemanticFleet.ConsoleSamples.Reliability;
/// <summary>
/// Represents an exception thrown when a configuration key is not found.
/// </summary>
public sealed class ConfigurationNotFoundException : Exception
{
    /// <summary>
    /// Gets the section value, which is a nullable string.
    /// </summary>
    public string? Section { get; }

    /// <summary>
    /// Gets the key of the string, which may be null.
    /// </summary>
    public string? Key { get; }

    /// <summary>
    /// Initializes a new instance of the ConfigurationNotFoundException class with the specified section and key.
    /// </summary>
    /// <param name="section">The section of the configuration key.</param>
    /// <param name="key">The key of the configuration key.</param>
    /// <returns>
    /// An instance of the ConfigurationNotFoundException class.
    /// </returns>
    public ConfigurationNotFoundException(string section, string key)
        : base($"Configuration key '{section}:{key}' not found")
    {
        this.Section = section;
        this.Key = key;
    }

    /// <summary>
    /// Initializes a new instance of the ConfigurationNotFoundException class with the specified section name.
    /// </summary>
    /// <param name="section">The name of the configuration section that was not found.</param>
    /// <returns>A new instance of the ConfigurationNotFoundException class.</returns>
    public ConfigurationNotFoundException(string section)
        : base($"Configuration section '{section}' not found")
    {
        this.Section = section;
    }

    /// <summary>
    /// Initializes a new instance of the ConfigurationNotFoundException class with no error message.
    /// </summary>
    /// <returns>
    /// The newly created ConfigurationNotFoundException object.
    /// </returns>
    public ConfigurationNotFoundException() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the ConfigurationNotFoundException class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public ConfigurationNotFoundException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
