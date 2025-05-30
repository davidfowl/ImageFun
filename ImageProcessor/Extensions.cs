using Azure.Storage.Blobs;
using Microsoft.Extensions.AI;

public static class Extensions
{
    public static void AddChatClient(this IHostApplicationBuilder builder)
    {
        builder.AddOpenAIClient("oai")
               .AddChatClient()
               .UseOpenTelemetry()
               .UseLogging();

        // This is the default name of the trace source and meter
        var telemetryName = "Experimental.Microsoft.Extensions.AI";

        builder.Services.AddOpenTelemetry()
               .WithTracing(t => t.AddSource(telemetryName))
               .WithMetrics(m => m.AddMeter(telemetryName));
    }

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