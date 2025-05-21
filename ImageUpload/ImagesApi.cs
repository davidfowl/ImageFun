public static class ImagesApi
{
    public static void MapImageApi(this WebApplication app)
    {
        // Endpoint to render a photo gallery of image blobs
        app.MapGet("/", async (BlobContainerClient container) =>
        {
            static bool IsImage(string ext) => ext is ".jpg" or ".jpeg" or ".png" or ".gif";

            var blobs = container.GetBlobsAsync();
            var items = new List<string>();

            await foreach (var item in blobs)
            {
                var ext = Path.GetExtension(item.Name).ToLowerInvariant();
                if (IsImage(ext))
                {
                    items.Add(item.Name);
                }
            }
            return new RazorComponentResult<PhotoGallery>(new { Blobs = items });
        });

        // Minimal POST endpoint for image upload
        app.MapPost("/upload", async (
            IFormFile file,
            BlobContainerClient container) =>
        {
            var blob = container.GetBlobClient(file.FileName);
            using var stream = file.OpenReadStream();
            await blob.UploadAsync(stream, overwrite: true);

            // Redirect to home after upload
            return Results.Redirect("/");
        })
        .DisableAntiforgery();

        // Endpoint to serve raw image bytes for thumbnails and previews
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

            var contentType = Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                _ => "application/octet-stream"
            };
            return Results.Stream(await blob.OpenReadAsync(), contentType);
        });

        // Endpoint to render an HTML preview page for a single image
        app.MapGet("/preview/{*path}", async Task<IResult> (string path, BlobContainerClient container) =>
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (string.IsNullOrEmpty(path) || ext is not (".jpg" or ".jpeg" or ".png" or ".gif"))
            {
                // Return a minimal HTML redirect since RazorComponentResult cannot be returned here
                return Results.Content("<meta http-equiv='refresh' content='0; url=/' />", "text/html");
            }
            var blob = container.GetBlobClient(path);
            if (!await blob.ExistsAsync())
            {
                return Results.NotFound();
            }
            // Render the ImagePreview Razor component
            return new RazorComponentResult<ImagePreview>(new { Path = path });
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
        })
        .DisableAntiforgery();

    }
}