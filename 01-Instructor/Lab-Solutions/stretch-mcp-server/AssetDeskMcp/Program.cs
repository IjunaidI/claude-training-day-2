// AssetDesk MCP server: lets Claude read and change AssetDesk through its HTTP API (SPEC.md §4.7).
// Transport is stdio: Claude Code starts this process and talks JSON-RPC over stdin/stdout.
// So stdout belongs to the protocol. Every log line goes to stderr.
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

var baseUrl = Environment.GetEnvironmentVariable("ASSETDESK_URL") is { Length: > 0 } url
    ? url
    : "http://localhost:5198";
builder.Services.AddSingleton(new HttpClient { BaseAddress = new Uri(baseUrl) });

builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<AssetDeskTools>();

await builder.Build().RunAsync();
