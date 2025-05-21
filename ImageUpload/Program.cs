var builder = WebApplication.CreateBuilder(args);

builder.AddAzureBlobContainerClient("images");

builder.Services.AddRazorComponents();

builder.AddServiceDefaults();

var app = builder.Build();

app.MapStaticAssets();

app.MapDefaultEndpoints();

app.MapImageApi();

app.MapDefaultEndpoints();

app.Run();
