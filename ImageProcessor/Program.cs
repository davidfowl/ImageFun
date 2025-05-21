using ImageProcessor;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddOpenAIClient("oai").AddChatClient();
builder.AddAzureBlobContainerClient("images");

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
