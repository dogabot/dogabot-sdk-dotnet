namespace Dogabot.Sdk;

/// <summary>Options for a single REST request.</summary>
public sealed class RequestOptions
{
    public IDictionary<string, string?>? Query { get; set; }
    public object? Body { get; set; }
    public IDictionary<string, string>? Headers { get; set; }
    /// <summary>Required for allowlist write mutations.</summary>
    public string? IdempotencyKey { get; set; }
    public CancellationToken CancellationToken { get; set; }
    internal bool Write { get; set; }
}
