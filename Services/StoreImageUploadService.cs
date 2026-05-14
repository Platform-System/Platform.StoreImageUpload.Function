using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using Platform.StoreImageUpload.Function.Configurations;
using Platform.StoreImageUpload.Function.Enums;
using Platform.StoreImageUpload.Function.Helpers;
using Platform.StoreImageUpload.Function.Models;

namespace Platform.StoreImageUpload.Function.Services;

public sealed class StoreImageUploadService
{
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];

    private readonly BlobStorageOptions _blobStorageOptions;

    public StoreImageUploadService(IOptions<BlobStorageOptions> blobStorageOptions)
    {
        _blobStorageOptions = blobStorageOptions.Value;
    }

    public async Task<UploadStoreImageResult> UploadAsync(
        Guid storeId,
        StoreImageType type,
        MultipartFileData file,
        CancellationToken cancellationToken)
    {
        if (!AllowedContentTypes.Contains(file.ContentType))
            throw new InvalidOperationException("Only JPEG, PNG, and WEBP images are allowed.");

        if (!ImageSignatureValidator.IsValid(file))
            throw new InvalidOperationException("File content does not match a supported image format.");

        if (string.IsNullOrWhiteSpace(_blobStorageOptions.ConnectionString))
            throw new InvalidOperationException("Blob storage connection string is not configured.");

        if (string.IsNullOrWhiteSpace(_blobStorageOptions.ContainerName))
            throw new InvalidOperationException("Blob storage container name is not configured.");

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension))
            throw new InvalidOperationException("File extension is required.");

        var generatedFileName = $"{Guid.NewGuid():N}{extension}";
        var typeSegment = type == StoreImageType.Avatar ? "avatar" : "cover";
        var blobName = $"stores/{storeId}/{typeSegment}/{generatedFileName}";

        var blobServiceClient = new BlobServiceClient(_blobStorageOptions.ConnectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(_blobStorageOptions.ContainerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);
        await containerClient.SetAccessPolicyAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        var blobClient = containerClient.GetBlobClient(blobName);

        await using (file.FileStream)
        {
            await blobClient.UploadAsync(
                file.FileStream,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = file.ContentType
                    }
                },
                cancellationToken);
        }

        return new UploadStoreImageResult
        {
            FileName = generatedFileName,
            BlobName = blobName,
            ContainerName = containerClient.Name,
            ContentType = file.ContentType,
            Size = file.FileSize,
            AltText = file.AltText,
            Url = blobClient.Uri.ToString()
        };
    }
}
