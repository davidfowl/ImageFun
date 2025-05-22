using Azure.Storage.Blobs;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddChatClient();

builder.AddAzureBlobs();

var app = builder.Build();

app.MapGet("/describe/{name}", async (string name, BlobContainerClient client, IChatClient chatClient) =>
{
    // Get the blob client for the image
    var blobClient = client.GetBlobClient(name);

    // Check if the blob exists
    if (!await blobClient.ExistsAsync())
    {
        return Results.NotFound();
    }

    AIContent imageContent = await DownloadBlobAsByteArray("image/png");

    async Task<DataContent> DownloadBlobAsByteArray(string contentType)
    {
        // Download the image bytes
        var response = await blobClient.DownloadAsync();
        var ms = new MemoryStream();
        await response.Value.Content.CopyToAsync(ms);

        return new DataContent(ms.ToArray(), contentType);
    }

    // Generate a description for the image using AI
    var chatResponse = await chatClient.GetResponseAsync(new ChatMessage()
    {
        Contents =
        [
            new TextContent("Generate a fun caption for this image:"),
            imageContent
        ]
    });

    return Results.Content(chatResponse.Text);
});

app.Run();
