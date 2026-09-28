using System.ComponentModel;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using ModelContextProtocol.Server;

// The two tools Claude sees. Each [McpServerTool] method becomes one tool; the Description
// attributes are the only documentation Claude gets about when and how to call it.
[McpServerToolType]
public sealed class AssetDeskTools
{
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // LAB 6: flaw 1. "Gets data." does not say what data, when to use this tool, or what the
    // filters accept. Claude chooses tools by reading this text, so it has to guess.
    [McpServerTool(Name = "list_assets"), Description("Gets data.")]
    public static async Task<string> ListAssets(HttpClient http, string? status = null, string? category = null)
    {
        var state = await http.GetFromJsonAsync<StateDto>("/api/state", Json)
            ?? throw new InvalidOperationException("GET /api/state returned no body.");

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

    [McpServerTool(Name = "assign_asset"), Description("Assigns an asset to an employee.")]
    public static async Task<string> AssignAsset(HttpClient http, string assetId, string employeeId)
    {
        var body = new
        {
            employeeId,
            assignedDate = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        };
        using var response = await http.PostAsJsonAsync(
            $"/api/assets/{Uri.EscapeDataString(assetId)}/assign", body, Json);

        if (!response.IsSuccessStatusCode)
        {
            // LAB 6: flaw 2. The API answered 400 with {"error": "..."} and a message written for
            // a person to act on (SPEC.md §4.6). This throws it away and invents its own copy.
            return "Something went wrong.";
        }

        return await response.Content.ReadAsStringAsync();
    }

    // "in_stock", "InStock" and "in stock" all match: builds differ in how they spell enums.
    static bool Same(string value, string filter) => Norm(value) == Norm(filter);
    static string Norm(string s) => s.Replace("_", "").Replace(" ", "").ToLowerInvariant();

    sealed record StateDto(List<EmployeeDto> Employees, List<AssetDto> Assets);
    sealed record EmployeeDto(string Id, string Name, string Department);
    sealed record AssetDto(
        string Id, string Tag, string Category, string Make, string Model, string Status,
        string Location, string? AssignedTo, string? AssignedDate);
}
