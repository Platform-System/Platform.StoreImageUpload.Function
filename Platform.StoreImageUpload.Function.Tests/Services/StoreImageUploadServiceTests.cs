using Microsoft.Extensions.Options;
using Platform.StoreImageUpload.Function.Configurations;
using Platform.StoreImageUpload.Function.Enums;
using Platform.StoreImageUpload.Function.Models;
using Platform.StoreImageUpload.Function.Services;
using Xunit;

namespace Platform.StoreImageUpload.Function.Tests.Services;

public sealed class StoreImageUploadServiceTests
{
    [Fact]
    public async Task UploadAsync_WhenContentTypeIsUnsupported_ThrowsInvalidOperation()
    {
        var service = CreateService(connectionString: "UseDevelopmentStorage=true", containerName: "stores");
        var file = CreateFile("store.gif", "image/gif", [0x47, 0x49, 0x46]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UploadAsync(Guid.NewGuid(), StoreImageType.Avatar, file, CancellationToken.None));

        Assert.Equal("Only JPEG, PNG, and WEBP images are allowed.", exception.Message);
    }

    [Fact]
    public async Task UploadAsync_WhenImageSignatureDoesNotMatch_ThrowsInvalidOperation()
    {
        var service = CreateService(connectionString: "UseDevelopmentStorage=true", containerName: "stores");
        var file = CreateFile("store.png", "image/png", [0x01, 0x02, 0x03]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UploadAsync(Guid.NewGuid(), StoreImageType.Cover, file, CancellationToken.None));

        Assert.Equal("File content does not match a supported image format.", exception.Message);
    }

    [Fact]
    public async Task UploadAsync_WhenConnectionStringMissing_ThrowsInvalidOperation()
    {
        var service = CreateService(connectionString: "", containerName: "stores");
        var file = CreateFile("store.png", "image/png", [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UploadAsync(Guid.NewGuid(), StoreImageType.Avatar, file, CancellationToken.None));

        Assert.Equal("Blob storage connection string is not configured.", exception.Message);
    }

    [Fact]
    public async Task UploadAsync_WhenContainerNameMissing_ThrowsInvalidOperation()
    {
        var service = CreateService(connectionString: "UseDevelopmentStorage=true", containerName: "");
        var file = CreateFile("store.webp", "image/webp", [0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UploadAsync(Guid.NewGuid(), StoreImageType.Cover, file, CancellationToken.None));

        Assert.Equal("Blob storage container name is not configured.", exception.Message);
    }

    private static StoreImageUploadService CreateService(string connectionString, string containerName)
        => new(Options.Create(new BlobStorageOptions
        {
            ConnectionString = connectionString,
            ContainerName = containerName
        }));

    private static MultipartFileData CreateFile(string fileName, string contentType, byte[] bytes)
        => new()
        {
            FileName = fileName,
            ContentType = contentType,
            FileSize = bytes.Length,
            FileStream = new MemoryStream(bytes),
            AltText = "store"
        };
}
