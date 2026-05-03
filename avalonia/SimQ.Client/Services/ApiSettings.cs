using System;

namespace SimQ.Client.Services;

/// <summary>
/// Runtime configuration for the SimQ WebApi connection. The base URL can be
/// overridden via the <c>SIMQ_API_URL</c> environment variable; otherwise it
/// defaults to the local dev server.
/// </summary>
public sealed class ApiSettings
{
    public const string DefaultBaseUrl = "http://localhost:5000/";

    public string BaseUrl { get; init; } = DefaultBaseUrl;
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(15);
    public TimeSpan HealthCheckInterval { get; init; } = TimeSpan.FromSeconds(5);

    public static ApiSettings FromEnvironment()
    {
        var url = Environment.GetEnvironmentVariable("SIMQ_API_URL");
        return new ApiSettings
        {
            BaseUrl = string.IsNullOrWhiteSpace(url) ? DefaultBaseUrl : EnsureSlash(url),
        };
    }

    private static string EnsureSlash(string url) => url.EndsWith('/') ? url : url + "/";
}
