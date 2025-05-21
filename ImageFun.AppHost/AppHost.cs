var builder = DistributedApplication.CreateBuilder(args);

var oaikey = builder.AddParameter("oaikey", secret: true);
var oaics = builder.AddConnectionString("oai", cs =>
{
    cs.Append($"Key={oaikey};Model=gpt-4o");
});

var storage = builder.AddAzureStorage("storage").RunAsEmulator();

var blobs = storage.AddBlobs("blobs");
var container = blobs.AddBlobContainer("images", blobContainerName: "image-uploads");

builder.AddProject<Projects.ImageUpload>("web")
    .WithExternalHttpEndpoints()
    .WithReference(container)
    .WaitFor(container);

builder.AddProject<Projects.ImageProcessor>("imageprocessor")
       .WithExternalHttpEndpoints()
       .WithReference(container)
       .WithReference(oaics)
       .WaitFor(container);

builder.Build().Run();
