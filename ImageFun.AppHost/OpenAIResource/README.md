# OpenAI Resource for .NET Aspire

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure OpenAI services.

## Getting started

### Prerequisites

- OpenAI account with API access
- OpenAI [API key](https://platform.openai.com/api-keys)

## Usage example

Then, in the _AppHost.cs_ file of `AppHost`, add an OpenAI resource and consume the connection using the following methods:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var openai = builder.AddOpenAI("openai", "gpt-4o");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(openai);

builder.Build().Run();
```

The `WithReference` method passes that connection information into a connection string named `openai` in the `MyService` project.

In the _Program.cs_ file of `MyService`, the connection can be consumed using a client library like [Azure.AI.OpenAI](https://www.nuget.org/packages/Azure.AI.OpenAI):

```csharp
var builder = WebApplication.CreateBuilder(args);

// Get the connection string
var openaiConnectionString = builder.Configuration.GetConnectionString("openai");

// Parse the connection string
var connectionParts = openaiConnectionString.Split(';')
    .Select(part => part.Split('='))
    .ToDictionary(parts => parts[0], parts => parts[1]);
    
var apiKey = connectionParts["Key"];
var model = connectionParts["Model"];
var endpoint = connectionParts.TryGetValue("Endpoint", out var ep) ? ep : "https://api.openai.com";

// Configure your OpenAI client
builder.Services.AddSingleton(new OpenAIClient(apiKey, new OpenAIClientOptions
{
    Endpoint = new Uri(endpoint)
}));
```

## Configuration

The OpenAI resource can be configured with the following options:

### API Key

The API key can be set as a configuration value using the default name `{resource_name}-apikey` or the `OPENAI_API_KEY` environment variable.

Then in user secrets:

```json
{
    "Parameters": 
    {
        "openai-apikey": "YOUR_OPENAI_API_KEY_HERE"
    }
}
```

Furthermore, the API key can be configured using a custom parameter:

```csharp
var apiKey = builder.AddParameter("my-api-key", secret: true);
var openai = builder.AddOpenAI("openai", "gpt-4o")
                    .WithApiKey(apiKey);
```

Then in user secrets:

```json
{
    "Parameters": 
    {
        "my-api-key": "YOUR_OPENAI_API_KEY_HERE"
    }
}
```

### Custom Endpoints

The resource supports custom endpoints for Azure OpenAI or other OpenAI-compatible services:

```csharp
// Azure OpenAI
var azureEndpoint = builder.AddParameter("azure-endpoint", "https://myazure.openai.azure.com");
var azureKey = builder.AddParameter("azure-key", secret: true);
var azureModel = builder.AddParameter("azure-model", "gpt-4");

var azureOpenai = builder.AddOpenAI("azure-openai", azureEndpoint, azureKey, azureModel);
```

### Fluent Configuration

Use fluent methods to customize the resource:

```csharp
var openai = builder.AddOpenAI("openai", "gpt-4")
    .WithModel("gpt-4o")
    .WithEndpoint("https://custom-endpoint.com");
```

## Connection String Format

The resource generates connection strings in the following format:

- **With endpoint**: `Endpoint=https://api.example.com;Key=sk-xxx;Model=gpt-4`
- **Without endpoint**: `Key=sk-xxx;Model=gpt-4` (uses default OpenAI endpoint)

## Available Models

OpenAI supports various AI models. Some popular options include:

- `gpt-4o`
- `gpt-4o-mini`
- `gpt-4`
- `gpt-3.5-turbo`

Check the [OpenAI documentation](https://platform.openai.com/docs/models) for the most up-to-date list of available models.

## Resource Properties

The OpenAI resource provides these properties:

- **Model**: The model name (e.g., "gpt-4o")
- **Key**: The API key parameter resource
- **Endpoint**: The endpoint parameter resource (optional)
- **ConnectionStringExpression**: The computed connection string expression

## Examples

See the `Examples.cs` file for comprehensive usage examples including:

- Basic OpenAI configuration
- Azure OpenAI setup
- Fluent configuration patterns
- Service consumption examples

## Additional documentation

* https://platform.openai.com/docs
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire
