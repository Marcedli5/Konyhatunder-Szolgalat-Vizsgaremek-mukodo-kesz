using System.Net.Http.Json;
using System.Text.Json;

namespace Frontend_ASP.Services;

public sealed class BackendApiClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient = httpClient;

    public async Task<T?> GetAsync<T>(string relativeUrl)
    {
        using var response = await _httpClient.GetAsync(relativeUrl);
        return await ReadAsync<T>(response);
    }

    public async Task<T?> PostAsync<TRequest, T>(string relativeUrl, TRequest request)
    {
        using var response = await _httpClient.PostAsJsonAsync(relativeUrl, request);
        return await ReadAsync<T>(response);
    }

    public async Task PostAsync<TRequest>(string relativeUrl, TRequest request)
    {
        using var response = await _httpClient.PostAsJsonAsync(relativeUrl, request);
        await EnsureSuccessAsync(response);
    }

    private static async Task<T?> ReadAsync<T>(HttpResponseMessage response)
    {
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var details = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException(
            $"Backend API hivas sikertelen. HTTP {(int)response.StatusCode}. {details}");
    }
}
