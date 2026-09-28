using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AssetDesk.Data;

// The only place an HTTP failure becomes an AssetDeskException. Components never
// see a status code and never write their own error copy (SPEC.md 4.6).
public class AssetDeskClient(HttpClient http)
{
    // Program.cs configures the server to write enums as strings. The default web
    // options have no enum converter, so without this every read of "status":"InStock"
    // fails. Both directions use it, so requests send "Repair" rather than 2.
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public Task<AppStateDto> GetStateAsync(CancellationToken ct) =>
        GetAsync<AppStateDto>("/api/state", ct);

    public Task<List<AssetDto>> CreateAssetsAsync(NewAssetInputDto input, CancellationToken ct) =>
        PostAsync<NewAssetInputDto, List<AssetDto>>("/api/assets", input, ct);

    public Task<AssetDto> AssignAsync(string assetId, string employeeId, string assignedDate, CancellationToken ct) =>
        PostAsync<AssignRequest, AssetDto>(
            $"/api/assets/{assetId}/assign", new AssignRequest(employeeId, assignedDate), ct);

    public Task<AssetDto> ReturnAsync(string assetId, CancellationToken ct) =>
        PostAsync<object?, AssetDto>($"/api/assets/{assetId}/return", null, ct);

    public Task<AssetDto> SetStatusAsync(string assetId, Status status, CancellationToken ct) =>
        PostAsync<StatusRequest, AssetDto>(
            $"/api/assets/{assetId}/status", new StatusRequest(status), ct);

    private async Task<TOut> GetAsync<TOut>(string path, CancellationToken ct)
    {
        using var response = await http.GetAsync(path, ct);
        return await ReadAsync<TOut>(response, ct);
    }

    private async Task<TOut> PostAsync<TIn, TOut>(string path, TIn body, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync(path, body, Json, ct);
        return await ReadAsync<TOut>(response, ct);
    }

    private static async Task<TOut> ReadAsync<TOut>(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            var problem = await response.Content.ReadFromJsonAsync<ApiError>(Json, ct);
            throw new AssetDeskException(
                problem?.Error ?? "The server could not complete that request. Refresh and try again.");
        }

        return await response.Content.ReadFromJsonAsync<TOut>(Json, ct)
               ?? throw new AssetDeskException("The server returned no data. Refresh to try again.");
    }
}
