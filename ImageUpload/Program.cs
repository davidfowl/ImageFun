var builder = WebApplication.CreateBuilder(args);

builder.AddAzureBlobs();

builder.Services.AddRazorComponents();

builder.Services.AddAntiforgery();

builder.AddServiceDefaults();

var app = builder.Build();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapImageApi();

app.MapDefaultEndpoints();

app.Run();
