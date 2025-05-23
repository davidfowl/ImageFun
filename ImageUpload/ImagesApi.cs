public static class ImagesApi
{
    public static void MapImageApi(this WebApplication app)
    {
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

        app.MapPost("/upload", async (
            IFormFile file,
            BlobContainerClient container) =>
        {
            var blob = container.GetBlobClient(file.FileName);
            using var stream = file.OpenReadStream();
            await blob.UploadAsync(stream, overwrite: true);

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

            var contentType = Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                _ => "application/octet-stream"
            };

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
            var encodedName = Uri.EscapeDataString(name);
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