using System.Net.Http.Json;
using PetroVisorLite.Web.Models;

namespace PetroVisorLite.Web.Services;

/// <summary>
/// Typed client for the PetroVisorLite backend API. Registered with a named/typed
/// <see cref="HttpClient"/> whose BaseAddress comes from configuration
/// (<c>wwwroot/appsettings.json</c>, key <c>BackendApi:BaseUrl</c>) — never hardcoded — and
/// which has <see cref="JwtAuthorizationHandler"/> attached so calls are authenticated on the
/// user's behalf automatically.
/// </summary>
public class PetroVisorApiClient
{
    private readonly HttpClient _httpClient;

    public PetroVisorApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<LoginResponse?> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", new LoginRequest(email, password), cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<WellDto>> GetWellsAsync(CancellationToken cancellationToken = default) =>
        await _httpClient.GetFromJsonAsync<List<WellDto>>("api/wells", cancellationToken) ?? new List<WellDto>();

    public async Task<WellDto?> GetWellAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _httpClient.GetFromJsonAsync<WellDto>($"api/wells/{id}", cancellationToken);

    public async Task<IReadOnlyList<FacilityDto>> GetFacilitiesAsync(CancellationToken cancellationToken = default) =>
        await _httpClient.GetFromJsonAsync<List<FacilityDto>>("api/facilities", cancellationToken) ?? new List<FacilityDto>();

    public async Task<IReadOnlyList<ProductionRecordDto>> GetProductionAsync(
        Guid wellId, DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (from is not null) query.Add($"from={from:yyyy-MM-dd}");
        if (to is not null) query.Add($"to={to:yyyy-MM-dd}");
        var qs = query.Count > 0 ? "?" + string.Join('&', query) : string.Empty;

        return await _httpClient.GetFromJsonAsync<List<ProductionRecordDto>>($"api/production/well/{wellId}{qs}", cancellationToken)
            ?? new List<ProductionRecordDto>();
    }

    public async Task<ProductionKpiDto?> GetProductionKpiAsync(
        Guid wellId, DateOnly periodStart, DateOnly periodEnd, CancellationToken cancellationToken = default) =>
        await _httpClient.GetFromJsonAsync<ProductionKpiDto>(
            $"api/kpi/wells/{wellId}/production?periodStart={periodStart:yyyy-MM-dd}&periodEnd={periodEnd:yyyy-MM-dd}", cancellationToken);

    public async Task<DashboardSummaryDto?> GetDashboardSummaryAsync(
        DateOnly periodStart, DateOnly periodEnd, CancellationToken cancellationToken = default) =>
        await _httpClient.GetFromJsonAsync<DashboardSummaryDto>(
            $"api/kpi/dashboard?periodStart={periodStart:yyyy-MM-dd}&periodEnd={periodEnd:yyyy-MM-dd}", cancellationToken);

    public async Task<AssistantQueryResponseDto?> QueryAssistantAsync(string question, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return null;
        }

        var response = await _httpClient.PostAsJsonAsync("api/assistant/query", new AssistantQueryRequest(question), cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<AssistantQueryResponseDto>(cancellationToken: cancellationToken);
    }

    public async Task<CsvImportResultDto?> ImportProductionCsvAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        content.Add(streamContent, "file", fileName);

        var response = await _httpClient.PostAsync("api/production/import", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<CsvImportResultDto>(cancellationToken: cancellationToken);
    }
}
