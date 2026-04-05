namespace BrandsAdvisory.Endpoints;

/// <summary>
/// Provides safe client configuration from server-side settings.
/// Only non-sensitive values that the Blazor WebAssembly client needs
/// at startup should be exposed here.
/// </summary>
public static class ConfigEndpoints
{
    public static void MapConfigEndpoints(this WebApplication app)
    {
        app.MapGet("/api/config", (IConfiguration config) =>
        {
            return Results.Ok(new ClientConfig
            {
                SyncfusionLicenseKey = config["Syncfusion:LicenseKey"] ?? string.Empty
            });
        })
        .AllowAnonymous()
        .WithName("GetClientConfig");
    }
}

public record ClientConfig
{
    public string SyncfusionLicenseKey { get; init; } = string.Empty;
}
