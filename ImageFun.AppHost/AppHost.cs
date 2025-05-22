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

// There's a bug https://github.com/dotnet/aspire/issues/9454
var containercs = builder.AddConnectionString("imagescs", cs =>
{
    if (storage.Resource.IsEmulator)
    {
        cs.Append($"Endpoint=\"{blobs}\";");
    }
    else
    {
        cs.Append($"Endpoint={blobs};");
    }
    cs.Append($"ContainerName={container.Resource.BlobContainerName}");
});

var acr = builder.AddAzureContainerRegistry("acr");
var feenv = builder.AddAzureAppServiceEnvironment("fe-env")
    .WithAzureContainerRegistry(acr);
var beenv = builder.AddAzureContainerAppEnvironment("be-env")
    .WithAzureContainerRegistry(acr);

var imageProcessor = builder.AddProject<Projects.ImageProcessor>("imageprocessor")
       .WithExternalHttpEndpoints()
       .WithReference(containercs)
       .WithReference(oaics)
       .WaitFor(containercs)
       .WithComputeEnvironment(beenv);

builder.AddProject<Projects.ImageUpload>("web")
    .WithExternalHttpEndpoints()
    .WithReference(containercs)
    .WaitFor(containercs)
    .WithReference(imageProcessor)
    .WaitFor(imageProcessor)
    .WithComputeEnvironment(feenv)
    .FixEndpoints();

builder.Build().Run();
