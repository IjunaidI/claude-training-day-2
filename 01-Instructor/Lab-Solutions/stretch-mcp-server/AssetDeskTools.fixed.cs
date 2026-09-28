using System.ComponentModel;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

// Stretch demo answer key. Drop-in replacement for AssetDeskMcp/AssetDeskTools.cs (tools/AssetDeskMcp/ in a demo clone).
// Fix 1: the descriptions say what each tool returns, when to use it, and what each parameter accepts.
// Fix 2: a refused call returns the API's own error message, verbatim, flagged isError: true.
[McpServerToolType]
public sealed class AssetDeskTools
{
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [McpServerTool(Name = "list_assets", ReadOnly = true), Description(
        "Lists AssetDesk hardware assets (laptops, monitors, headsets, docks, phones, keyboards) " +
        "with each asset's id, tag (e.g. AST-1001), make, model, status, location and current " +
        "holder. Also returns every employee's id, name and department. Use it to answer what " +
        "we own, who has what, and what is in stock, and to look up the asset id and employee " +
        "id that assign_asset needs. Filters are optional and combine with AND.")]
    public static async Task<string> ListAssets(
        HttpClient http,
        [Description("Only assets with this status: InStock, Assigned, Repair or Retired. Omit for all.")]
        string? status = null,
        [Description("Only assets in this category: Laptop, Monitor, Headset, Dock, Phone, Keyboard or Other. Omit for all.")]
        string? category = null)
    {
        var state = await http.GetFromJsonAsync<StateDto>("/api/state", Json)
            ?? throw new McpException("GET /api/state returned no body.");

        var names = state.Employees.ToDictionary(e => e.Id, e => e.Name);
        var assets = state.Assets
            .Where(a => string.IsNullOrWhiteSpace(status) || Same(a.Status, status))
            .Where(a => string.IsNullOrWhiteSpace(category) || Same(a.Category, category))
            .Select(a => new
            {
                a.Id, a.Tag, a.Category, a.Make, a.Model, a.Status, a.Location,
                AssignedTo = a.AssignedTo is { } id && names.TryGetValue(id, out var n) ? n : null,
                a.AssignedDate,
            });
        var employees = state.Employees.Select(e => new { e.Id, e.Name, e.Department });

        return JsonSerializer.Serialize(new { assets, employees }, Json);
    }

    [McpServerTool(Name = "assign_asset"), Description(
        "Assigns one in-stock asset to one employee, dated today, and returns the updated asset. " +
        "Only assets with status InStock can be assigned. Call list_assets first to find both ids. " +
        "If AssetDesk refuses, the error text is AssetDesk's own message: report it to the user " +
        "as written and do not retry with a different asset unless the user asks.")]
    public static async Task<CallToolResult> AssignAsset(
        HttpClient http,
        [Description("The asset's id from list_assets (a GUID). Not its tag: pass the id, not \"AST-1003\".")]
        string assetId,
        [Description("The employee's id from list_assets (a GUID). Not their name or email.")]
        string employeeId)
    {
        var body = new
        {
            employeeId,
            assignedDate = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        };
        using var response = await http.PostAsJsonAsync(
            $"/api/assets/{Uri.EscapeDataString(assetId)}/assign", body, Json);

        var text = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode)
            return Result(text, isError: false);

        // AssetDesk answers 400 with {"error": "..."} (SPEC.md §4.7), and that message is written for
        // a person to act on (§4.6). Pass it through unchanged. Return the error result ourselves:
        // a thrown McpException also reaches Claude, but prefixed "An error occurred invoking ...".
        var error = text.StartsWith('{') ? JsonSerializer.Deserialize<ErrorDto>(text, Json)?.Error : null;
        return Result(error ?? $"AssetDesk returned HTTP {(int)response.StatusCode}.", isError: true);
    }

    static CallToolResult Result(string text, bool isError) =>
        new() { Content = [new TextContentBlock { Text = text }], IsError = isError };

    // "in_stock", "InStock" and "in stock" all match: builds differ in how they spell enums.
    static bool Same(string value, string filter) => Norm(value) == Norm(filter);
    static string Norm(string s) => s.Replace("_", "").Replace(" ", "").ToLowerInvariant();

    sealed record StateDto(List<EmployeeDto> Employees, List<AssetDto> Assets);
    sealed record EmployeeDto(string Id, string Name, string Department);
    sealed record AssetDto(
        string Id, string Tag, string Category, string Make, string Model, string Status,
        string Location, string? AssignedTo, string? AssignedDate);
    sealed record ErrorDto(string? Error);
}
