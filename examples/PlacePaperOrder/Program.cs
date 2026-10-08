// Place a tiny paper market order (Idempotency-Key required).
// Dry-run unless CONFIRM_PLACE=1. trading_mode stays paper.
using System.Text.Json;
using Dogabot.Sdk;

if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOGABOT_API_KEY")))
{
    Console.Error.WriteLine("Set DOGABOT_API_KEY=dbk_live_...");
    return 1;
}

var body = new
{
    exchange = Environment.GetEnvironmentVariable("DOGABOT_EXCHANGE") ?? "hyperliquid",
    symbol = Environment.GetEnvironmentVariable("DOGABOT_SYMBOL") ?? "BTC",
    side = "buy",
    quantity = 0.001,
    order_type = "market",
    trading_mode = "paper",
    broadcast_mode = "personal",
};
var idem = Environment.GetEnvironmentVariable("DOGABOT_IDEMPOTENCY_KEY")
    ?? $"example-paper-{Guid.NewGuid():N}";

if (Environment.GetEnvironmentVariable("CONFIRM_PLACE") != "1")
{
    Console.WriteLine("Dry-run only. Would POST placeorder with:");
    Console.WriteLine(JsonSerializer.Serialize(new { body, idempotency_key = idem }, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine("Re-run with CONFIRM_PLACE=1 to submit (still paper).");
    return 0;
}

using var client = new Client();
var outJson = await client.PostTerminalPlaceorderAsync(
    body,
    new RequestOptions { IdempotencyKey = idem });
Console.WriteLine(JsonSerializer.Serialize(outJson, new JsonSerializerOptions { WriteIndented = true }));
return 0;
