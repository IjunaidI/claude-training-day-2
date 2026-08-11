using System.Text.Json.Serialization;
using AssetDesk.Components;
using AssetDesk.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString = Db.ResolveConnectionString(builder.Environment.ContentRootPath);

builder.Services.AddSingleton(new AssetRepository(connectionString));
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

Db.Initialize(connectionString);

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

app.MapGet("/api/health", () => Results.Ok(new { ok = true }));

app.MapGet("/api/state", (AssetRepository repo) => Results.Ok(repo.GetState()));

app.MapPost("/api/assets", (NewAssetInput input, AssetRepository repo) =>
    Results.Json(repo.CreateAssets(input), statusCode: StatusCodes.Status201Created));

app.MapPost("/api/assets/{id}/assign", (string id, AssignRequest body, AssetRepository repo) =>
    Results.Ok(repo.Assign(id, body.EmployeeId, body.AssignedDate)));

app.MapPost("/api/assets/{id}/return", (string id, AssetRepository repo) =>
    Results.Ok(repo.Return(id)));

app.MapPost("/api/assets/{id}/status", (string id, StatusRequest body, AssetRepository repo) =>
    Results.Ok(repo.SetStatus(id, body.Status)));

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

record AssignRequest(string EmployeeId, string AssignedDate);
record StatusRequest(Status Status);
