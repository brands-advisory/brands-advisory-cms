using Azure.Identity;
using BrandsAdvisory.Components;
using Microsoft.AspNetCore.DataProtection;
using BrandsAdvisory.Core.Interfaces;
using BrandsAdvisory.Core.Services;
using BrandsAdvisory.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Azure.Cosmos;
using Microsoft.Identity.Web;
using Syncfusion.Blazor;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSyncfusionBlazor();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCascadingAuthenticationState();

// Required for GitHub Codespaces and other reverse proxy environments:
// Trust X-Forwarded-Proto and X-Forwarded-Host so ASP.NET Core knows
// the request is HTTPS, which is needed for Secure cookies to work.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedProto
        | ForwardedHeaders.XForwardedHost;
    // Clear default restrictions so the Codespaces proxy IP is trusted
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var dataProtectionBuilder = builder.Services.AddDataProtection()
    .SetApplicationName("brands-advisory-cms");

// Persist DataProtection keys to disk in development so that
// dotnet watch restarts don't invalidate in-flight OIDC correlation cookies.
if (builder.Environment.IsDevelopment())
{
    var keysDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".aspnet", "DataProtection-Keys", "brands-advisory-cms");
    Directory.CreateDirectory(keysDir);
    dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(keysDir));
}

builder.Services.AddMicrosoftIdentityWebAppAuthentication(builder.Configuration);

// Fix for GitHub Codespaces and local HTTP development.
// Uses PostConfigure so our settings run AFTER Microsoft.Identity.Web's
// own PostConfigure, which would otherwise override Configure calls.
//
// Root cause of "Correlation failed":
//   ASP.NET Core sets CorrelationCookie with SameSite=None by default.
//   Chrome 80+ rejects SameSite=None cookies that lack the Secure attribute.
//   Over HTTP (localhost dev), SecurePolicy=SameAsRequest omits Secure,
//   so Chrome silently drops the cookie → no correlation cookie on callback.
//
// Fix: SameSite=Lax is accepted by all browsers without requiring Secure,
//   and covers the OIDC code flow (callback is a plain GET redirect).
//   SecurePolicy=SameAsRequest adds Secure automatically on HTTPS (production).
builder.Services.PostConfigure<CookieAuthenticationOptions>(
    CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

builder.Services.PostConfigure<OpenIdConnectOptions>(
    OpenIdConnectDefaults.AuthenticationScheme, options =>
    {
        options.NonceCookie.SameSite = SameSiteMode.Lax;
        options.NonceCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

        // Fix for GitHub Codespaces: ASP.NET Core binds to 127.0.0.1
        // internally but Entra ID only accepts localhost for http URIs.
        // Chain after Microsoft.Identity.Web's existing handler (if any).
        options.Events ??= new OpenIdConnectEvents();
        var existingRedirectHandler = options.Events.OnRedirectToIdentityProvider;
        options.Events.OnRedirectToIdentityProvider = async context =>
        {
            if (existingRedirectHandler != null)
                await existingRedirectHandler(context);

            if (context.ProtocolMessage.RedirectUri?.Contains("127.0.0.1") == true)
            {
                context.ProtocolMessage.RedirectUri =
                    context.ProtocolMessage.RedirectUri
                        .Replace("127.0.0.1", "localhost");
            }
        };
    });

builder.Services.AddAuthorization();

// Cosmos DB client (singleton, thread-safe)
// Authentication via DefaultAzureCredential:
//   - locally: Azure CLI credentials (az login)
//   - production: System-Assigned Managed Identity
builder.Services.AddSingleton(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    return new CosmosClient(
        cfg["CosmosDb:EndpointUri"]!,
        new DefaultAzureCredential(),
        new CosmosClientOptions
        {
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            }
        });
});

// Repositories
builder.Services.AddSingleton<IArticleRepository, ArticleRepository>();
builder.Services.AddSingleton<IProjectRepository, ProjectRepository>();
builder.Services.AddSingleton<IAboutRepository, AboutRepository>();

builder.Services.AddScoped<IOwnerService, OwnerService>();

var app = builder.Build();

// Configure the HTTP request pipeline.

// Must be first in the pipeline so all subsequent middleware
// (including HTTPS redirect and authentication) sees the correct scheme.
app.UseForwardedHeaders();

// Normalize 127.0.0.1 → localhost so the OIDC correlation cookie
// is set and sent back with the same host in both the login request
// and the Entra ID callback. Without this, the browser stores the
// cookie for 127.0.0.1 but the callback arrives at localhost (or
// vice versa), causing "Correlation failed".
app.Use(async (context, next) =>
{
    if (context.Request.Host.Host == "127.0.0.1")
    {
        context.Request.Host = new HostString(
            "localhost",
            context.Request.Host.Port ?? 5000);
    }
    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Login: redirect to Microsoft login
app.MapGet("/login", (string? returnUrl) =>
{
    var redirectUri = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl;
    return Results.Challenge(
        new AuthenticationProperties { RedirectUri = redirectUri },
        [OpenIdConnectDefaults.AuthenticationScheme]);
});

// Logout: sign out locally and from Azure AD
app.MapPost("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme,
        new AuthenticationProperties { RedirectUri = "/" });
});

app.Run();
