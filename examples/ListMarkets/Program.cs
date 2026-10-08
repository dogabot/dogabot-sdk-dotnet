// List markets with optional filters (GET /api/v1/markets).
using System.Text.Json;
using Dogabot.Sdk;

if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOGABOT_API_KEY")))
{
    Console.Error.WriteLine("Set DOGABOT_API_KEY=dbk_live_...");
    return 1;
}

using var client = new Client();
var markets = await client.GetMarketsAsync(new RequestOptions
{
    Query = new Dictionary<string, string?>
    {
        ["limit"] = "5",
        ["is_tradable"] = "true",
    },
});
Console.WriteLine(JsonSerializer.Serialize(markets, new JsonSerializerOptions { WriteIndented = true }));
return 0;
