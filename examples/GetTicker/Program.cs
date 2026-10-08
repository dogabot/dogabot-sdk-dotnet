// Fetch a ticker with query params (GET /api/v1/ticker).
using System.Text.Json;
using Dogabot.Sdk;

if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOGABOT_API_KEY")))
{
    Console.Error.WriteLine("Set DOGABOT_API_KEY=dbk_live_...");
    return 1;
}

var exchange = Environment.GetEnvironmentVariable("DOGABOT_EXCHANGE") ?? "hyperliquid";
var symbol = Environment.GetEnvironmentVariable("DOGABOT_SYMBOL") ?? "BTC";

using var client = new Client();
var ticker = await client.GetTickerAsync(new RequestOptions
{
    Query = new Dictionary<string, string?>
    {
        ["exchange"] = exchange,
        ["symbol"] = symbol,
    },
});
Console.WriteLine(JsonSerializer.Serialize(ticker, new JsonSerializerOptions { WriteIndented = true }));
return 0;
