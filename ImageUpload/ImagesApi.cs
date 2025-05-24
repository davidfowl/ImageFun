using Azure.Storage.Blobs.Models;

public static class ImagesApi
{
    public static void MapImageApi(this WebApplication app)
    {
        app.MapGet("/", async (BlobContainerClient container) =>
        {
            var blobs = container.GetBlobsAsync();
            var images = new List<string>();

            await foreach (var item in blobs)
            {
                images.Add(item.Name);
            }

            return new RazorComponentResult<PhotoGallery>(new { Images = images });
        });
        
        app.MapPost("/upload", async (
            IFormFile file,
            BlobContainerClient container) =>
        {
            var contentType = Path.GetExtension(file.FileName).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                _ => null
            };

            if (contentType == null)
            {
                return Results.BadRequest("Unsupported file type.");
            }

            var blob = container.GetBlobClient(file.FileName);
            using var stream = file.OpenReadStream();
            await blob.UploadAsync(stream, overwrite: true);
            await blob.SetHttpHeadersAsync(new BlobHttpHeaders
            {
                ContentType = contentType
            });

            // Redirect to home after upload
            return Results.Redirect("/");
        });

        app.MapGet("/images/{*path}", async (string path, BlobContainerClient container) =>
        {
            if (string.IsNullOrEmpty(path))
            {
                return Results.NotFound();
            }

            var blob = container.GetBlobClient(path);
            if (!await blob.ExistsAsync())
            {
                return Results.NotFound();
            }

            async Task<string> GetContentTypeAsync()
            {
                var props = await blob.GetPropertiesAsync();

                if (props.Value.ContentType != null)
                {
                    return props.Value.ContentType;
                }

                return Path.GetExtension(path).ToLowerInvariant() switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    _ => "application/octet-stream"
                };
            }

            var contentType = await GetContentTypeAsync();

            return Results.Stream(await blob.OpenReadAsync(), contentType);
        });

        app.MapGet("/preview/{*path}", async Task<IResult> (string path, BlobContainerClient container) =>
        {
            var blob = container.GetBlobClient(path);
            if (!await blob.ExistsAsync())
            {
                return Results.NotFound();
            }

            // Render the ImagePreview Razor component
            return new RazorComponentResult<ImagePreview>(new { Path = path });
        });

        app.MapGet("/describe/{name}", async (string name, HttpClient client) =>
        {
            // Forward the request to the image processor service
            var encodedName = UrlPathEncode(name);
            return await client.GetStringAsync($"http+https://imageprocessor/describe/{encodedName}");
        });

        // Endpoint to delete a blob
        app.MapPost("/delete", async ([FromForm] string file, BlobContainerClient container) =>
        {
            if (!string.IsNullOrEmpty(file))
            {
                var blob = container.GetBlobClient(file);
                await blob.DeleteIfExistsAsync();
            }
            return Results.Redirect("/");
        });
    }
}