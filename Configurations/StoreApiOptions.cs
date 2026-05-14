namespace Platform.StoreImageUpload.Function.Configurations;

public sealed class StoreApiOptions
{
    public const string SectionName = "Integrations:Store";

    public string Address { get; set; } = string.Empty;
}
