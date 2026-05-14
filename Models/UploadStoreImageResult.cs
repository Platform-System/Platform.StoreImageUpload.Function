namespace Platform.StoreImageUpload.Function.Models;

public sealed class UploadStoreImageResult
{
    public string FileName { get; set; } = string.Empty;
    public string BlobName { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string AltText { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}
