var builder = DistributedApplication.CreateBuilder(args);

var openaikey = builder.AddParameter("oaikey", secret: true);
var model = builder.AddParameter("model", "gpt-4.1", publishValueAsDefault: true);

// Add a model connection
var oai = builder.AddOpenAIConnection("oai", openaikey, model);

var storage = builder.AddAzureStorage("storage").RunAsEmulator();

// This will make sure the container is created
var container = storage.AddBlobContainer("images", blobContainerName: "image-uploads");

var acr = builder.AddAzureContainerRegistry("acr");

var feenv = builder.AddAzureAppServiceEnvironment("fe-env")
    .WithAzureContainerRegistry(acr);

var beenv = builder.AddAzureContainerAppEnvironment("be-env")
    .WithAzureContainerRegistry(acr);

var imageProcessor = builder.AddProject<Projects.ImageProcessor>("imageprocessor")
       .WithExternalHttpEndpoints()
       .WithReference(container)
       .WithReference(oai)
       .WaitFor(container)
       .WithComputeEnvironment(beenv);

builder.AddProject<Projects.ImageUpload>("web")
    .WithExternalHttpEndpoints()
    .WithReference(container)
    .WaitFor(container)
    .WithReference(imageProcessor)
    .WaitFor(imageProcessor)
    .WithComputeEnvironment(feenv)
    .FixEndpoints();

builder.Build().Run();
