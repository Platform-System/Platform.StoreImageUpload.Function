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
            .Bind(context.Configuration.GetSection(BlobStorageOptions.SectionName));

        services
            .AddOptions<StoreApiOptions>()
            .Bind(context.Configuration.GetSection(StoreApiOptions.SectionName));

        services.AddHttpClient<StoreApiClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<StoreApiOptions>>().Value;
            client.BaseAddress = string.IsNullOrWhiteSpace(options.Address)
                ? new Uri("http://localhost")
                : new Uri(options.Address);
        });

        services.AddSingleton<StoreImageUploadService>();
    })
    .Build();

await host.RunAsync();
