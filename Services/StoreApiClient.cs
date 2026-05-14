using Microsoft.Extensions.Options;
using Platform.StoreImageUpload.Function.Configurations;
using Platform.StoreImageUpload.Function.Enums;
using Platform.StoreImageUpload.Function.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Platform.StoreImageUpload.Function.Services;

public sealed class StoreApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public StoreApiClient(HttpClient httpClient, IOptions<StoreApiOptions> options)
    {
        var address = options.Value.Address?.Trim();
        if (!string.IsNullOrWhiteSpace(address))
            httpClient.BaseAddress = new Uri(address);

        _httpClient = httpClient;
    }

    public async Task<(bool IsSuccess, Guid? StoreId, HttpStatusCode StatusCode, string? Error)> GetCurrentStoreAsync(
        string bearerToken,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/store/manage/stores/me");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            return (false, null, response.StatusCode, string.IsNullOrWhiteSpace(error) ? "Unable to get current store." : error);
        }

        var payload = await response.Content.ReadFromJsonAsync<CurrentStoreResponse>(JsonOptions, cancellationToken);
        if (payload?.Success != true || payload.Data?.Profile is null)
            return (false, null, response.StatusCode, payload?.Errors.FirstOrDefault() ?? "Current store payload is invalid.");

        return (true, payload.Data.Profile.Id, response.StatusCode, null);
    }

    public async Task<(bool IsSuccess, HttpStatusCode StatusCode, string? Error)> SetStoreImageAsync(
        string bearerToken,
        StoreImageType type,
        UploadStoreImageResult uploadResult,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/store/manage/stores/me/images/{type.ToString().ToLowerInvariant()}");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
        request.Content = JsonContent.Create(new
        {
            uploadResult.BlobName,
            uploadResult.ContainerName,
            uploadResult.FileName,
            uploadResult.ContentType,
            uploadResult.Size,
            uploadResult.AltText,
            uploadResult.Url
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
            return (true, response.StatusCode, null);

        var error = await response.Content.ReadAsStringAsync(cancellationToken);
        return (false, response.StatusCode, string.IsNullOrWhiteSpace(error) ? "Unable to save store image." : error);
    }
}
