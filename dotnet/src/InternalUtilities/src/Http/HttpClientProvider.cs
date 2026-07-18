#pragma warning disable IDE0073
// Copyright (c) Microsoft. All rights reserved.
#pragma warning restore IDE0073

using System.Net.Http;
using Microsoft.Extensions.Logging;

/// <summary>
/// Provides functionality for retrieving instances of HttpClient.
/// </summary>
internal static class HttpClientProvider
{
    /// <summary>
    /// Retrieves an instance of HttpClient.
    /// </summary>
    /// <param name="httpClient">An optional pre-existing instance of HttpClient.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use for logging. If null, no logging will be performed.</param>
    /// <returns>An instance of HttpClient.</returns>
#pragma warning disable IDE0060 // Remove unused parameter (loggerFactory kept for caller API stability)
    public static HttpClient GetHttpClient(HttpClient? httpClient, ILoggerFactory? loggerFactory)
#pragma warning restore IDE0060
    {
        if (httpClient is null)
        {
            // We refrain from disposing the underlying SK default HttpClient handler as it would impact other HTTP clients that utilize the same handler.
            return new HttpClient(NonDisposableHttpClientHandler.Instance, disposeHandler: false);
        }

        return httpClient;
    }
}
