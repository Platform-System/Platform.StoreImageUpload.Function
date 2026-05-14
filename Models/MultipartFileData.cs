namespace Platform.StoreImageUpload.Function.Models;

public sealed class MultipartFileData
{
    public Stream FileStream { get; set; } = Stream.Null;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string AltText { get; set; } = string.Empty;
}
