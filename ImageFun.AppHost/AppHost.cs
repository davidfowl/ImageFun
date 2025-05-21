var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage").RunAsEmulator();

var blobs = storage.AddBlobs("blobs");
var container = blobs.AddBlobContainer("images", blobContainerName: "image-uploads");

builder.AddProject<Projects.ImageUpload>("web")
    .WithExternalHttpEndpoints()
    .WithReference(container)
    .WaitFor(container);

builder.Build().Run();
