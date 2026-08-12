using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AssetDesk.Data;

public class AssetDeskClient(HttpClient http)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task<AppStateDto> GetStateAsync(CancellationToken ct)
    {
        var response = await http.GetAsync("/api/state", ct);
        return await ReadOrThrowAsync<AppStateDto>(response, ct);
    }

    public async Task<List<AssetDto>> CreateAssetsAsync(NewAssetInput input, CancellationToken ct)
    {
        var response = await http.PostAsJsonAsync("/api/assets", input, JsonOptions, ct);
        return await ReadOrThrowAsync<List<AssetDto>>(response, ct);
    }

    public async Task<AssetDto> AssignAsync(string assetId, string employeeId, string assignedDate, CancellationToken ct)
    {
        var response = await http.PostAsJsonAsync(
            $"/api/assets/{assetId}/assign", new AssignRequest(employeeId, assignedDate), JsonOptions, ct);
        return await ReadOrThrowAsync<AssetDto>(response, ct);
    }

    public async Task<AssetDto> ReturnAsync(string assetId, CancellationToken ct)
    {
        var response = await http.PostAsync($"/api/assets/{assetId}/return", content: null, ct);
        return await ReadOrThrowAsync<AssetDto>(response, ct);
    }

    public async Task<AssetDto> SetStatusAsync(string assetId, Status status, CancellationToken ct)
    {
        var response = await http.PostAsJsonAsync(
            $"/api/assets/{assetId}/status", new StatusRequest(status), JsonOptions, ct);
        return await ReadOrThrowAsync<AssetDto>(response, ct);
    }

    private static async Task<T> ReadOrThrowAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions, ct);
            throw new AssetDeskException(error?.Error ?? "Something went wrong. Refresh to see current data.");
        }

        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct))!;
    }
}
