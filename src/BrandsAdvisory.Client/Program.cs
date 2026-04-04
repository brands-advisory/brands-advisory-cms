using BrandsAdvisory.Client.Services;
using BrandsAdvisory.Core.Interfaces;
using BrandsAdvisory.Core.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Syncfusion.Blazor;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Register Syncfusion Community License key.
// Set value in wwwroot/appsettings.json: { "Syncfusion": { "LicenseKey": "..." } }
var syncfusionKey = builder.Configuration["Syncfusion:LicenseKey"];
if (!string.IsNullOrEmpty(syncfusionKey) && !syncfusionKey.StartsWith("__"))
    Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(syncfusionKey);

builder.Services.AddSyncfusionBlazor();
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

// HttpClient pointing to the host server (same origin — cookies are sent automatically)
builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Authentication state: reads current user from /api/user on the host server
builder.Services.AddScoped<AuthenticationStateProvider, ApiAuthenticationStateProvider>();

// Repositories: call minimal API endpoints on the host server
builder.Services.AddScoped<IAboutRepository, HttpAboutRepository>();
builder.Services.AddScoped<IProjectRepository, HttpProjectRepository>();
builder.Services.AddScoped<IArticleRepository, HttpArticleRepository>();

// OwnerService: same logic as server-side — checks user.IsInRole("SiteAdmin")
builder.Services.AddScoped<IOwnerService, OwnerService>();

await builder.Build().RunAsync();
