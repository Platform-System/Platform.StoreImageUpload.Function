using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Platform.StoreImageUpload.Function.Configurations;
using Platform.StoreImageUpload.Function.Services;
using Xunit;

namespace Platform.StoreImageUpload.Function.Tests.Services;

public sealed class StoreApiClientRegistrationTests
{
    [Fact]
    public void TypedClientRegistration_UsesConfiguredBaseAddress()
    {
        var services = new ServiceCollection();
        services.AddOptions<StoreApiOptions>()
            .Configure(options => options.Address = "https://store-api.example");

        services.AddHttpClient<StoreApiClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<StoreApiOptions>>().Value;
            client.BaseAddress = string.IsNullOrWhiteSpace(options.Address)
                ? new Uri("http://localhost")
                : new Uri(options.Address);
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var client = scope.ServiceProvider.GetRequiredService<StoreApiClient>();
        var httpClient = GetHttpClient(client);

        Assert.Equal(new Uri("https://store-api.example/"), httpClient.BaseAddress);
    }

    private static HttpClient GetHttpClient(StoreApiClient client)
    {
        var field = typeof(StoreApiClient).GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(field);
        return Assert.IsType<HttpClient>(field!.GetValue(client));
    }
}
