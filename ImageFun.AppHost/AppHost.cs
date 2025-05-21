var builder = DistributedApplication.CreateBuilder(args);

var oaikey = builder.AddParameter("oaikey", secret: true);
var oaics = builder.AddConnectionString("oai", cs =>
{
    cs.Append($"Key={oaikey};Model=gpt-4o");
});

var storage = builder.AddAzureStorage("storage").RunAsEmulator();

var blobs = storage.AddBlobs("blobs");
var container = blobs.AddBlobContainer("images", blobContainerName: "image-uploads");

var imageProcessor = builder.AddProject<Projects.ImageProcessor>("image-processor")
       .WithExternalHttpEndpoints()
       .WithReference(container)
       .WithReference(oaics)
       .WaitFor(container);

builder.AddProject<Projects.ImageUpload>("web")
    .WithExternalHttpEndpoints()
    .WithReference(container)
    .WaitFor(container)
    .WithReference(imageProcessor)
    .WaitFor(imageProcessor);

builder.Build().Run();
