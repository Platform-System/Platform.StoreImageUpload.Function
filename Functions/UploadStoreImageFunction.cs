using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Options;
using Platform.StoreImageUpload.Function.Configurations;
using Platform.StoreImageUpload.Function.Enums;
using Platform.StoreImageUpload.Function.Helpers;
using Platform.StoreImageUpload.Function.Services;
using System.Net;

namespace Platform.StoreImageUpload.Function.Functions;

public sealed class UploadStoreImageFunction
{
    private readonly StoreImageUploadService _storeImageUploadService;
    private readonly BlobStorageOptions _blobStorageOptions;
    private readonly StoreApiClient _storeApiClient;

    public UploadStoreImageFunction(
        StoreImageUploadService storeImageUploadService,
        IOptions<BlobStorageOptions> blobStorageOptions,
        StoreApiClient storeApiClient)
    {
        _storeImageUploadService = storeImageUploadService;
        _blobStorageOptions = blobStorageOptions.Value;
        _storeApiClient = storeApiClient;
    }

    [Function(nameof(UploadStoreImageFunction))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "stores/me/images/{type}")]
        HttpRequestData request,
        string type,
        CancellationToken cancellationToken)
    {
        var token = request.GetBearerToken();
        if (token is null)
        {
            var unauthorized = request.CreateResponse(HttpStatusCode.Unauthorized);
            await unauthorized.WriteStringAsync("Unauthorized.", cancellationToken);
            return unauthorized;
        }

        if (!TryParseStoreImageType(type, out var imageType))
            return await HttpRequestHelpers.CreateBadRequestAsync(request, "Store image type must be avatar or cover.", cancellationToken);

        var currentStore = await _storeApiClient.GetCurrentStoreAsync(
            token,
            cancellationToken);

        if (!currentStore.IsSuccess || currentStore.StoreId is null)
        {
            var denied = request.CreateResponse(currentStore.StatusCode);
            await denied.WriteStringAsync(currentStore.Error ?? "Forbidden.", cancellationToken);
            return denied;
        }

        var (file, readError) = await MultipartFileReader.ReadSingleFileAsync(request, cancellationToken);
        if (readError is not null || file is null)
            return await HttpRequestHelpers.CreateBadRequestAsync(request, readError!, cancellationToken);

        if (file.FileSize == 0)
            return await HttpRequestHelpers.CreateBadRequestAsync(request, "File is empty.", cancellationToken);

        var maxFileSizeInBytes = (long)_blobStorageOptions.MaxFileSizeInMb * 1024 * 1024;
        if (file.FileSize > maxFileSizeInBytes)
        {
            return await HttpRequestHelpers.CreateBadRequestAsync(
                request,
                $"File size must not exceed {_blobStorageOptions.MaxFileSizeInMb} MB.",
                cancellationToken);
        }

        try
        {
            var result = await _storeImageUploadService.UploadAsync(
                currentStore.StoreId.Value,
                imageType,
                file,
                cancellationToken);

            var setImageResult = await _storeApiClient.SetStoreImageAsync(
                token,
                imageType,
                result,
                cancellationToken);

            if (!setImageResult.IsSuccess)
            {
                var failed = request.CreateResponse(setImageResult.StatusCode);
                await failed.WriteStringAsync(setImageResult.Error ?? "Unable to save store image.", cancellationToken);
                return failed;
            }

            var okResponse = request.CreateResponse(HttpStatusCode.OK);
            await okResponse.WriteAsJsonAsync(result, cancellationToken);
            return okResponse;
        }
        catch (InvalidOperationException ex)
        {
            return await HttpRequestHelpers.CreateBadRequestAsync(request, ex.Message, cancellationToken);
        }
        catch (Exception)
        {
            var internalError = request.CreateResponse(HttpStatusCode.InternalServerError);
            await internalError.WriteStringAsync("Unable to upload store image.", cancellationToken);
            return internalError;
        }
    }

    private static bool TryParseStoreImageType(string rawType, out StoreImageType type)
    {
        if (string.Equals(rawType, "avatar", StringComparison.OrdinalIgnoreCase))
        {
            type = StoreImageType.Avatar;
            return true;
        }

        if (string.Equals(rawType, "cover", StringComparison.OrdinalIgnoreCase))
        {
            type = StoreImageType.Cover;
            return true;
        }

        type = default;
        return false;
    }
}
