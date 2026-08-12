using AssetDesk.Components;
using AssetDesk.Data;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var dataDir = Path.Combine(builder.Environment.ContentRootPath, "data");
Directory.CreateDirectory(dataDir);
var dbPath = Path.Combine(dataDir, "assetdesk.db");
var connectionString = Db.BuildConnectionString(dbPath);

Db.Initialize(connectionString);

builder.Services.AddSingleton(new AssetRepository(connectionString));

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
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
        await context.Response.WriteAsJsonAsync(new ErrorResponse(ex.Message));
    }
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

var api = app.MapGroup("/api");

api.MapGet("/health", () => Results.Ok(new { ok = true }));

api.MapGet("/state", (AssetRepository repo) => Results.Ok(repo.GetState().ToDto()));

api.MapPost("/assets", (NewAssetInput input, AssetRepository repo) =>
{
    var created = repo.CreateAssets(input);
    return Results.Json(created.Select(a => a.ToDto()).ToList(), statusCode: StatusCodes.Status201Created);
});

api.MapPost("/assets/{id}/assign", (string id, AssignRequest body, AssetRepository repo) =>
    Results.Ok(repo.Assign(id, body.EmployeeId, body.AssignedDate).ToDto()));

api.MapPost("/assets/{id}/return", (string id, AssetRepository repo) =>
    Results.Ok(repo.Return(id).ToDto()));

api.MapPost("/assets/{id}/status", (string id, StatusRequest body, AssetRepository repo) =>
    Results.Ok(repo.SetStatus(id, body.Status).ToDto()));

app.Run();
