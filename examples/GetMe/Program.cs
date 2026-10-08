// Print the authenticated account (GET /api/v1/me).
using System.Text.Json;
using Dogabot.Sdk;

if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOGABOT_API_KEY")))
{
    Console.Error.WriteLine("Set DOGABOT_API_KEY=dbk_live_...");
    return 1;
}

using var client = new Client();
var me = await client.GetMeAsync();
Console.WriteLine(JsonSerializer.Serialize(me, new JsonSerializerOptions { WriteIndented = true }));
return 0;
