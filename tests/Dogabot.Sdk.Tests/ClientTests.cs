using System.Net;
using System.Text;
using System.Text.Json;
using Dogabot.Sdk;
using Xunit;

namespace Dogabot.Sdk.Tests;

public class ClientTests
{
    private sealed class ScriptedHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage> Handler { get; set; } =
            _ => new HttpResponseMessage(HttpStatusCode.OK);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(Handler(request));
    }

    [Fact]
    public async Task GetMe_Success()
    {
        var handler = new ScriptedHandler
        {
            Handler = req =>
            {
                Assert.Equal("/api/v1/me", req.RequestUri!.AbsolutePath);
                Assert.Equal("Bearer dbk_live_test", req.Headers.Authorization!.ToString());
                Assert.StartsWith("dogabot-sdk-dotnet/", req.Headers.UserAgent.ToString());
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"data":{"user_id":"u1"}}""",
                        Encoding.UTF8,
                        "application/json"),
                };
            },
        };
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://example.test") };
        using var client = new Client(apiKey: "dbk_live_test", baseUrl: "http://example.test", httpClient: http);
        var me = await client.GetMeAsync();
        Assert.Equal("u1", me.GetProperty("data").GetProperty("user_id").GetString());
    }

    [Fact]
    public async Task Write_RequiresIdempotency()
    {
        using var client = new Client(apiKey: "dbk_live_test", baseUrl: "http://example.test");
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            client.PostTerminalPlaceorderAsync(new { symbol = "BTC" }));
        Assert.Contains("Idempotency-Key", ex.Message);
    }

    [Fact]
    public async Task Write_SendsIdempotencyKey()
    {
        var handler = new ScriptedHandler
        {
            Handler = req =>
            {
                Assert.True(req.Headers.TryGetValues("Idempotency-Key", out var vals));
                Assert.Equal("k1", vals!.Single());
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"data":{"ok":true}}""", Encoding.UTF8, "application/json"),
                };
            },
        };
        using var http = new HttpClient(handler);
        using var client = new Client(apiKey: "dbk_live_test", baseUrl: "http://example.test", httpClient: http);
        await client.PostTerminalPlaceorderAsync(
            new { symbol = "BTC" },
            new RequestOptions { IdempotencyKey = "k1" });
    }

    [Fact]
    public async Task MapsApiError()
    {
        var handler = new ScriptedHandler
        {
            Handler = _ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("""{"error":"unauthorized"}""", Encoding.UTF8, "application/json"),
            },
        };
        using var http = new HttpClient(handler);
        using var client = new Client(
            apiKey: "dbk_live_test",
            baseUrl: "http://example.test",
            httpClient: http,
            maxRetries: 0);
        var ex = await Assert.ThrowsAsync<ApiError>(() => client.GetMeAsync());
        Assert.Equal(401, ex.StatusCode);
        Assert.Equal("unauthorized", ex.Message);
    }

    [Fact]
    public void OperationIds_CoverAllowlist()
    {
        Assert.True(Client.OperationIds.Length >= 100);
        foreach (var id in Client.OperationIds)
        {
            var method = char.ToUpperInvariant(id[0]) + id[1..] + "Async";
            Assert.NotNull(typeof(Client).GetMethod(method));
        }
    }
}
