public static class Extensions
{

    public static void AddAzureBlobs(this IHostApplicationBuilder builder)
    {
        builder.AddAzureBlobContainerClient("images");
    }
}