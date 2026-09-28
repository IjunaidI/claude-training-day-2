using System.Text.Json.Serialization;
using AssetDesk.Components;
using AssetDesk.Data;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var dataDir = Path.Combine(builder.Environment.ContentRootPath, "data");
Directory.CreateDirectory(dataDir);
var dbPath = Path.Combine(dataDir, "assetdesk.db");
var connectionString = Db.BuildConnectionString(dbPath);
Db.Initialize(connectionString);

builder.Services.AddSingleton(new AssetRepository(connectionString));

// The app calls its own endpoints, so the base address is not known until the
// server is listening. Resolve it from the server rather than hardcoding a port:
// the scaffold writes a random one into launchSettings.json.
builder.Services.AddHttpClient<AssetDeskClient>((sp, c) =>
{
    var addresses = sp.GetRequiredService<IServer>()
        .Features.Get<IServerAddressesFeature>()?.Addresses;
    var baseUrl = addresses?.FirstOrDefault() ?? "http://localhost:5198";
    c.BaseAddress = new Uri(baseUrl
        .Replace("[::]", "localhost")
        .Replace("0.0.0.0", "localhost"));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
app.UseHttpsRedirection();

app.UseAntiforgery();

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (AssetDeskException ex)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new { error = ex.Message });
    }
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapGet("/api/health", () => Results.Ok(new { ok = true }));

app.MapGet("/api/state", (AssetRepository repo) =>
{
    var state = repo.GetState();
    return Results.Ok(new AppStateDto(
        state.Employees.Select(e => Map.ToDto(e)).ToList(),
        state.Assets.Select(a => Map.ToDto(a)).ToList()));
});

app.MapPost("/api/assets", (NewAssetInputDto body, AssetRepository repo) =>
{
    var created = repo.CreateAssets(new NewAssetInput(
        body.Tag, body.Category, body.Make, body.Model, body.Serial, body.Condition,
        body.PurchaseDate, body.Cost, body.Location, body.Notes, body.Quantity));

    return Results.Json(
        created.Select(a => Map.ToDto(a)).ToList(),
        statusCode: StatusCodes.Status201Created);
});

app.MapPost("/api/assets/{id}/assign", (string id, AssignRequest body, AssetRepository repo) =>
    Results.Ok(Map.ToDto(repo.Assign(id, body.EmployeeId, body.AssignedDate))));

app.MapPost("/api/assets/{id}/return", (string id, AssetRepository repo) =>
    Results.Ok(Map.ToDto(repo.Return(id))));

app.MapPost("/api/assets/{id}/status", (string id, StatusRequest body, AssetRepository repo) =>
    Results.Ok(Map.ToDto(repo.SetStatus(id, body.Status))));

app.Run();

// Domain record -> boundary DTO, in one place. Local functions cannot be
// overloaded, so these live in a type rather than beside the statements above.
static class Map
{
    public static AssetDto ToDto(Asset a) => new(
        a.Id, a.Tag, a.Category, a.Make, a.Model, a.Serial, a.Status, a.Condition,
        a.PurchaseDate, a.Cost, a.Location, a.Notes, a.AssignedTo, a.AssignedDate);

    public static EmployeeDto ToDto(Employee e) => new(e.Id, e.Name, e.Email, e.Department, e.Title);
}
