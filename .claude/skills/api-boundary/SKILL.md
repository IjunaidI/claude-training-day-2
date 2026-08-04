---
name: api-boundary
description: Use when building or changing AssetDesk UI or data access - requires the UI to reach data only through the HTTP API and never through AssetRepository directly.
---

# The UI talks to the API, never to the repository

AssetDesk keeps one boundary: components speak HTTP, the repository speaks SQL, and nothing crosses.

## Rules

1. **No component references `AssetRepository`.** Not `@inject`, not `[Inject]`, not a parameter
   type, not a `using`. The repository is constructed in `Program.cs` and reachable only from the
   endpoint handlers there.
2. **Every read and write goes through `/api/*`** — the six endpoints in `SPEC.md` §4.7. If the UI
   needs data, an endpoint serves it.
3. **One typed client, registered in DI.** `AssetDeskClient` wraps `HttpClient`. Never
   `new HttpClient()` at a call site: it bypasses your configuration and exhausts sockets under
   reconnects.
4. **Every call takes a `CancellationToken`** and checks the status code before deserialising. A
   Blazor circuit that drops mid-request must not leave the call running.
5. **DTOs at the boundary.** The records in `Data/Models.cs` are the database's shape. Components
   take DTOs from `Data/Dtos.cs`. A domain record must never appear in a `[Parameter]` property or a
   component's method signature.
6. **Translate errors once, in the client.** A 400 carries `{ "error": "..." }`. The client turns it
   into an `AssetDeskException` carrying that exact message. Components render the message verbatim
   and never write their own copy — `SPEC.md` §4.6 requires it.

## Event handlers become async

HTTP is asynchronous and there is no synchronous escape that does not risk deadlock. Handlers become
`async Task`; await the client call, then reassign state.

`SPEC.md` §3.4 still requires synchronous Dapper **inside the repository** — that rule is about the
data layer's own calls, not about the UI's HTTP hop. Both are correct at the same time.

## Registering the client

The app calls its own endpoints, so the base address is not known until the server is listening.
Resolve it from the server instead of hardcoding a port:

```csharp
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

builder.Services.AddHttpClient<AssetDeskClient>((sp, c) =>
{
    var addresses = sp.GetRequiredService<IServer>()
        .Features.Get<IServerAddressesFeature>()?.Addresses;
    var baseUrl = addresses?.FirstOrDefault() ?? "http://localhost:5198";
    c.BaseAddress = new Uri(baseUrl
        .Replace("[::]", "localhost")
        .Replace("0.0.0.0", "localhost"));
});
```

Hardcoding `http://localhost:5198` breaks the moment anyone passes a different `--urls`, and the
scaffold writes a random port into `launchSettings.json`.

**One trap.** `app.UseHttpsRedirection()` sits in front of your own endpoints. When only an http
address is bound — which is what `--urls http://localhost:5198` does — ASP.NET logs
`Failed to determine the https port for redirect` and skips redirecting, so the self-call works. That
log line is expected and is not a build warning. If you bind https as well, the self-call must target
the http address or it will chase a redirect to a port with no dev certificate.

## What this does not change

Everything else in `SPEC.md` §3.4 stands: one connection per repository method, a transaction per
mutation, schema init once in `Program.cs`. This skill moves the UI's data path and nothing else.
