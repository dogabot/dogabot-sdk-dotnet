using System.Net;

namespace Dogabot.Sdk;

/// <summary>Non-success HTTP response from the dogabot API.</summary>
public sealed class ApiError : Exception
{
    public int StatusCode { get; }
    public string? ErrorBody { get; }
    public HttpResponseHeadersSnapshot Headers { get; }

    public ApiError(string message, int statusCode, string? errorBody, HttpResponseHeadersSnapshot headers)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorBody = errorBody;
        Headers = headers;
    }
}

/// <summary>Minimal header snapshot for errors (avoids leaking HttpResponseMessage lifetime).</summary>
public sealed class HttpResponseHeadersSnapshot
{
    public string? RetryAfter { get; init; }

    public static HttpResponseHeadersSnapshot From(HttpResponseMessage response) =>
        new() { RetryAfter = response.Headers.RetryAfter?.ToString() };
}
