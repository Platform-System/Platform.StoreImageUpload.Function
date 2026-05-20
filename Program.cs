using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Platform.StoreImageUpload.Function.Configurations;
using Platform.StoreImageUpload.Function.Services;
using System.Text.Json;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.Configure<JsonSerializerOptions>(options =>
        {
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });

        services
            .AddOptions<BlobStorageOptions>()
            .Bind(context.Configuration.GetSection(BlobStorageOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.ConnectionString), "BlobStorage:ConnectionString is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ContainerName), "BlobStorage:ContainerName is required.")
            .Validate(options => options.MaxFileSizeInMb > 0, "BlobStorage:MaxFileSizeInMb must be greater than 0.")
            .ValidateOnStart();

        services
            .AddOptions<StoreApiOptions>()
            .Bind(context.Configuration.GetSection(StoreApiOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Address), "StoreApi:Address is required.")
            .ValidateOnStart();

        services.AddHttpClient<StoreApiClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<StoreApiOptions>>().Value;
            client.BaseAddress = new Uri(options.Address);
        });

        services.AddSingleton<StoreImageUploadService>();
    })
    .Build();

await host.RunAsync();
