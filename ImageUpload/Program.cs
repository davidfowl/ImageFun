var builder = WebApplication.CreateBuilder(args);

builder.AddAzureBlobs();

builder.Services.AddRazorComponents();

builder.AddServiceDefaults();

var app = builder.Build();

app.MapStaticAssets();

app.MapDefaultEndpoints();

app.MapImageApi();

app.MapDefaultEndpoints();

app.Run();
