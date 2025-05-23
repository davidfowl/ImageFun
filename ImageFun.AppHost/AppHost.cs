var builder = DistributedApplication.CreateBuilder(args);

var oaikey = builder.AddParameter("oaikey", secret: true);
var oaics = builder.AddConnectionString("oai", cs =>
{
    cs.Append($"Key={oaikey};Model=gpt-4o");
});

var storage = builder.AddAzureStorage("storage").RunAsEmulator();

var blobs = storage.AddBlobs("blobs");

// This will make sure the container is created
var container = blobs.AddBlobContainer("images", blobContainerName: "image-uploads");

var acr = builder.AddAzureContainerRegistry("acr");
var feenv = builder.AddAzureAppServiceEnvironment("fe-env")
    .WithAzureContainerRegistry(acr);
var beenv = builder.AddAzureContainerAppEnvironment("be-env")
    .WithAzureContainerRegistry(acr);

var imageProcessor = builder.AddProject<Projects.ImageProcessor>("imageprocessor")
       .WithExternalHttpEndpoints()
       .WithReference(blobs)
       .WithReference(oaics)
       .WaitFor(container)
       .WithComputeEnvironment(beenv);

builder.AddProject<Projects.ImageUpload>("web")
    .WithExternalHttpEndpoints()
    .WithReference(blobs)
    .WaitFor(container)
    .WithReference(imageProcessor)
    .WaitFor(imageProcessor)
    .WithComputeEnvironment(feenv)
    .FixEndpoints();

builder.Build().Run();
