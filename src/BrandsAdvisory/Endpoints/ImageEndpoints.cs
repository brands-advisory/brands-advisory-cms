using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace BrandsAdvisory.Endpoints;

/// <summary>
/// Minimal API endpoint for uploading article images to Azure Blob Storage.
/// Images are stored in the 'article-images' container and served publicly.
/// </summary>
public static class ImageEndpoints
{
    public static IEndpointRouteBuilder MapImageEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/images/upload",
            async (IFormFile file, IConfiguration config, DefaultAzureCredential credential) =>
            {
                var endpoint = config["Storage:BlobEndpoint"];
                if (string.IsNullOrEmpty(endpoint))
                    return Results.Problem("Storage endpoint is not configured.");

                var blobServiceClient = new BlobServiceClient(new Uri(endpoint), credential);
                var containerClient = blobServiceClient.GetBlobContainerClient("article-images");

                var blobName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var blobClient = containerClient.GetBlobClient(blobName);

                await using var stream = file.OpenReadStream();
                await blobClient.UploadAsync(stream, new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType }
                });

                return Results.Ok(new { url = blobClient.Uri.ToString() });
            })
            .RequireAuthorization(p => p.RequireRole("SiteAdmin"))
            .DisableAntiforgery();

        return app;
    }
}
