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
            async (HttpContext context, IConfiguration config, ILoggerFactory loggerFactory) =>
            {
                var logger = loggerFactory.CreateLogger("ImageEndpoints");
                var file = context.Request.Form.Files.FirstOrDefault();

                if (file is null || file.Length == 0)
                    return Results.BadRequest("No file received.");

                var endpoint = config["Storage:BlobEndpoint"];
                if (string.IsNullOrEmpty(endpoint))
                    return Results.Problem("Storage endpoint is not configured.");

                try
                {
                    var credential = new DefaultAzureCredential();
                    var blobServiceClient = new BlobServiceClient(new Uri(endpoint), credential);
                    var containerClient = blobServiceClient.GetBlobContainerClient("article-images");

                    var blobName = Path.GetFileName(file.FileName);
                    var blobClient = containerClient.GetBlobClient(blobName);

                    await using var stream = file.OpenReadStream();
                    await blobClient.UploadAsync(stream, new BlobUploadOptions
                    {
                        HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType }
                    });

                    return Results.Text(blobClient.Uri.ToString());
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Image upload to Azure Blob Storage failed.");
                    return Results.Problem("Image upload failed. Check server logs for details.");
                }
            })
            .RequireAuthorization(p => p.RequireRole("SiteAdmin"))
            .DisableAntiforgery();

        return app;
    }
}
