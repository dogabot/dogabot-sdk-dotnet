using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Dogabot.Sdk;

/// <summary>dogabot public REST API client (API-key allowlist).</summary>
public sealed partial class Client : IDisposable
{
    public const string DefaultBaseUrl = "https://api.dogabot.com";
    private const string SdkVersion = "0.1.0";

    private readonly HttpClient _http;
    private readonly bool _ownsHttp;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly int _maxRetries;
    private readonly string _userAgent;

    public Client(
        string? apiKey = null,
        string? baseUrl = null,
        HttpClient? httpClient = null,
        int maxRetries = 2,
        string? userAgent = null)
    {
        _apiKey = apiKey ?? Environment.GetEnvironmentVariable("DOGABOT_API_KEY")
            ?? throw new ArgumentException("apiKey is required (pass apiKey or set DOGABOT_API_KEY)");
        _baseUrl = (baseUrl ?? DefaultBaseUrl).TrimEnd('/');
        _maxRetries = maxRetries;
        _userAgent = userAgent ?? $"dogabot-sdk-dotnet/{SdkVersion}";
        if (httpClient is null)
        {
            _http = new HttpClient();
            _ownsHttp = true;
        }
        else
        {
            _http = httpClient;
            _ownsHttp = false;
        }
    }

    internal async Task<JsonElement> RequestAsync(
        HttpMethod method,
        string path,
        RequestOptions? options = null,
        object? body = null)
    {
        options ??= new RequestOptions();
        if (body is not null)
        {
            options.Body = body;
        }
        if (options.Write && string.IsNullOrEmpty(options.IdempotencyKey))
        {
            throw new ArgumentException($"Idempotency-Key is required for write operation {method} {path}");
        }

        var url = path.StartsWith("http", StringComparison.Ordinal)
            ? path
            : _baseUrl + path;
        if (options.Query is { Count: > 0 })
        {
            var qs = string.Join("&", options.Query
                .Where(kv => kv.Value is not null)
                .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}"));
            url += (url.Contains('?', StringComparison.Ordinal) ? "&" : "?") + qs;
        }

        var attempt = 0;
        while (true)
        {
            using var req = new HttpRequestMessage(method, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            req.Headers.TryAddWithoutValidation("User-Agent", _userAgent);
            if (!string.IsNullOrEmpty(options.IdempotencyKey))
            {
                req.Headers.TryAddWithoutValidation("Idempotency-Key", options.IdempotencyKey);
            }
            if (options.Headers is not null)
            {
                foreach (var (k, v) in options.Headers)
                {
                    req.Headers.TryAddWithoutValidation(k, v);
                }
            }

            var payload = options.Body;
            if (payload is not null)
            {
                var json = JsonSerializer.Serialize(payload);
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            using var res = await _http.SendAsync(req, options.CancellationToken).ConfigureAwait(false);
            var text = await res.Content.ReadAsStringAsync(options.CancellationToken).ConfigureAwait(false);

            if (res.IsSuccessStatusCode)
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    return default;
                }
                using var doc = JsonDocument.Parse(text);
                return doc.RootElement.Clone();
            }

            var retryable = (int)res.StatusCode == 429 || (int)res.StatusCode >= 500;
            if (retryable && attempt < _maxRetries)
            {
                attempt++;
                var wait = TimeSpan.FromSeconds(Math.Min(1 << attempt, 8));
                if (res.Headers.RetryAfter?.Delta is TimeSpan delta)
                {
                    wait = delta;
                }
                else if (res.Headers.RetryAfter?.Date is DateTimeOffset date)
                {
                    wait = date - DateTimeOffset.UtcNow;
                    if (wait < TimeSpan.Zero) wait = TimeSpan.FromSeconds(1);
                }
                await Task.Delay(wait, options.CancellationToken).ConfigureAwait(false);
                continue;
            }

            string message = $"HTTP {(int)res.StatusCode} {method} {path}";
            try
            {
                using var errDoc = JsonDocument.Parse(text);
                if (errDoc.RootElement.TryGetProperty("error", out var err) && err.ValueKind == JsonValueKind.String)
                {
                    message = err.GetString() ?? message;
                }
            }
            catch (JsonException)
            {
                // keep default message
            }

            throw new ApiError(message, (int)res.StatusCode, text, HttpResponseHeadersSnapshot.From(res));
        }
    }

    public void Dispose()
    {
        if (_ownsHttp)
        {
            _http.Dispose();
        }
    }
}
