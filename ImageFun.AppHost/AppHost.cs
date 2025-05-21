var builder = DistributedApplication.CreateBuilder(args);

var oaikey = builder.AddParameter("oaikey", secret: true);
var oaics = builder.AddConnectionString("oai", cs =>
{
    cs.Append($"Key={oaikey};Model=gpt-4o");
});

var storage = builder.AddAzureStorage("storage").RunAsEmulator();

var blobs = storage.AddBlobs("blobs");
var container = blobs.AddBlobContainer("images", blobContainerName: "image-uploads");

var feEnv = builder.AddAzureAppServiceEnvironment("fe-env");
var beEnv = builder.AddAzureContainerAppEnvironment("be-env");
var imageProcessor = builder.AddProject<Projects.ImageProcessor>("image-processor")
       .WithExternalHttpEndpoints()
       .WithReference(container)
       .WithReference(oaics)
       .WaitFor(container)
       .WithComputeEnvironment(beEnv);

builder.AddProject<Projects.ImageUpload>("web")
    .WithExternalHttpEndpoints()
    .WithReference(container)
    .WaitFor(container)
    .WithReference(imageProcessor)
    .WaitFor(imageProcessor)
    .WithComputeEnvironment(feEnv)
    .FixEndpoint(beEnv);

builder.Build().Run();
