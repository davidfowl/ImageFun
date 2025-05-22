var builder = WebApplication.CreateBuilder(args);

builder.AddAzureBlobContainerClient("imagescs");

builder.Services.AddRazorComponents();

builder.AddServiceDefaults();

var app = builder.Build();

app.MapStaticAssets();

app.MapDefaultEndpoints();

app.MapImageApi();

app.MapDefaultEndpoints();

app.Run();
