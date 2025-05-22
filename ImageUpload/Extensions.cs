public static class Extensions
{

    public static void AddAzureBlobs(this IHostApplicationBuilder builder)
    {
        // There's a bug https://github.com/dotnet/aspire/issues/9454
        // builder.AddAzureBlobContainerClient("images");

        builder.AddAzureBlobClient("blobs");
        builder.Services.AddSingleton(sp =>
        {
            var client = sp.GetRequiredService<BlobServiceClient>();
            return client.GetBlobContainerClient("image-uploads");
        });
    }
}