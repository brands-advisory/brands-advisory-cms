using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace BrandsAdvisory.Endpoints;

/// <summary>
/// Minimal API endpoint for uploading article images to Azure Blob Storage.
/// Images are resized to a maximum of 1920px on the longest side and capped at 2 MB.
/// Each blob is stored under a sanitized GUID-based name.
/// </summary>
public static class ImageEndpoints
{
    private const int MaxFileSizeBytes = 2 * 1024 * 1024; // 2 MB
    private const int MaxDimension = 1920;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/jpg", "image/png", "image/webp", "image/gif"
    };

    public static IEndpointRouteBuilder MapImageEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/images/upload",
            async (HttpContext context, IConfiguration config, ILoggerFactory loggerFactory) =>
            {
                var logger = loggerFactory.CreateLogger("ImageEndpoints");
                var file = context.Request.Form.Files.FirstOrDefault();

                if (file is null || file.Length == 0)
                    return Results.BadRequest("No file received.");

                if (!AllowedContentTypes.Contains(file.ContentType))
                    return Results.BadRequest("File type not allowed.");

                var endpoint = config["Storage:BlobEndpoint"];
                if (string.IsNullOrEmpty(endpoint))
                    return Results.Problem("Storage endpoint is not configured.");

                try
                {
                    var (processedStream, contentType, extension) = await ProcessImageAsync(file, logger);
                    await using (processedStream)
                    {
                        var blobName = $"{Guid.NewGuid():N}{extension}";

                        var credential = new DefaultAzureCredential();
                        var blobServiceClient = new BlobServiceClient(new Uri(endpoint), credential);
                        var containerClient = blobServiceClient.GetBlobContainerClient("article-images");
                        var blobClient = containerClient.GetBlobClient(blobName);

                        await blobClient.UploadAsync(processedStream, new BlobUploadOptions
                        {
                            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
                        });

                        // Expose the final GUID blob URL as a response header so the
                        // Blazor WASM client can correct the img src that Syncfusion
                        // constructs from Path + originalFileName (which would be wrong).
                        context.Response.Headers["X-Blob-Url"] = blobClient.Uri.ToString();
                        return Results.Text(blobClient.Uri.ToString());
                    }
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

    /// <summary>
    /// Resizes the image to at most <see cref="MaxDimension"/> px on the longest side
    /// and re-encodes it. PNG stays PNG; everything else is converted to JPEG at quality 85.
    /// Logs a warning when the result still exceeds <see cref="MaxFileSizeBytes"/>.
    /// </summary>
    private static async Task<(MemoryStream Stream, string ContentType, string Extension)> ProcessImageAsync(
        IFormFile file, ILogger logger)
    {
        await using var inputStream = file.OpenReadStream();
        using var image = await Image.LoadAsync(inputStream);

        if (image.Width > MaxDimension || image.Height > MaxDimension)
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(MaxDimension, MaxDimension)
            }));
        }

        var outputStream = new MemoryStream();
        string contentType;
        string extension;

        if (file.ContentType.Equals("image/png", StringComparison.OrdinalIgnoreCase))
        {
            await image.SaveAsPngAsync(outputStream, new PngEncoder());
            contentType = "image/png";
            extension = ".png";
        }
        else
        {
            await image.SaveAsJpegAsync(outputStream, new JpegEncoder { Quality = 85 });
            contentType = "image/jpeg";
            extension = ".jpg";
        }

        if (outputStream.Length > MaxFileSizeBytes)
        {
            logger.LogWarning(
                "Processed image still exceeds {LimitMB} MB: {ActualKB} KB.",
                MaxFileSizeBytes / 1024 / 1024,
                outputStream.Length / 1024);
        }

        outputStream.Position = 0;
        return (outputStream, contentType, extension);
    }
}
