using System.Text.Json.Serialization;

namespace Platform.StoreImageUpload.Function.Models;

public sealed class CurrentStoreResponse
{
    public bool Success { get; init; }
    public CurrentStoreData? Data { get; init; }
    public IReadOnlyCollection<string> Errors { get; init; } = [];
}

public sealed class CurrentStoreData
{
    public CurrentStoreProfile? Profile { get; init; }
}

public sealed class CurrentStoreProfile
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }
}
