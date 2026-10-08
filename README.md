# dogabot-sdk-dotnet

Official .NET / C# client for the [dogabot public API](https://docs.dogabot.com/).

```bash
# NuGet.org publish pending — until then, clone and reference the project:
#   git clone https://github.com/dogabot/dogabot-sdk-dotnet
#   dotnet add reference path/to/dogabot-sdk-dotnet/src/Dogabot.Sdk/Dogabot.Sdk.csproj
# After NuGet.org: dotnet add package Dogabot.Sdk
```

```csharp
using Dogabot.Sdk;

using var client = new Client(apiKey: Environment.GetEnvironmentVariable("DOGABOT_API_KEY"));
var me = await client.GetMeAsync();
Console.WriteLine(me);
```

Writes require an idempotency key:

```csharp
await client.PostTerminalPlaceorderAsync(
    new {
        exchange = "hyperliquid",
        symbol = "BTC",
        side = "buy",
        quantity = 0.001,
        order_type = "market",
        trading_mode = "paper",
        broadcast_mode = "personal",
    },
    new RequestOptions { IdempotencyKey = "place-paper-btc-1" });
```

**Backend-first.** Do not embed `dbk_live_…` API keys in browser or client apps.

Runnable samples: [`examples/`](./examples/).

Docs: https://docs.dogabot.com/sdk/ · Repo: https://github.com/dogabot/dogabot-sdk-dotnet
