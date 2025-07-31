# OpenAI Resource for .NET Aspire

Provides extension methods and resource definitions for a .NET Aspire AppHost to configure OpenAI services as connection string resources.

## Getting started

### Prerequisites

- OpenAI account with API access
- OpenAI [API key](https://platform.openai.com/api-keys)

### Install the resource

Copy the `OpenAIResource.cs` file to your AppHost project and add the namespace:

```csharp
using ImageFun.AppHost.Resources;
```

## Usage example

In the _AppHost.cs_ file of `AppHost`, add an OpenAI resource and consume the connection using the following methods:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var openai = builder.AddOpenAI("openai", "gpt-4o");

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(openai);

builder.Build().Run();
```

The `WithReference` method passes that connection information into a connection string named `openai` in the `MyService` project.

In the _Program.cs_ file of `MyService`, the connection can be consumed using the [Aspire.OpenAI](https://www.nuget.org/packages/Aspire.OpenAI) library:

```csharp
builder.AddOpenAIClient("openai");
```

You can then retrieve the `OpenAIClient` instance using dependency injection. For example, to use the client in a minimal API:

```csharp
app.MapPost("/chat", async (string message, OpenAIClient client) =>
{
    var chatClient = client.GetChatClient("gpt-4o");
    var response = await chatClient.CompleteChatAsync([new UserChatMessage(message)]);
    return response.Value.Content[0].Text;
});
```

Alternatively, you can use [Microsoft.Extensions.AI](https://www.nuget.org/packages/Microsoft.Extensions.AI) for a more abstracted approach:

```csharp
builder.AddOpenAIClient("openai")
       .AddChatClient();

app.MapPost("/chat", async (string message, IChatClient chatClient) =>
{
    var response = await chatClient.CompleteAsync(message);
    return response.Message.Text;
});
```

To learn how to use the OpenAI client library refer to [Using the OpenAIClient class](https://github.com/openai/openai-dotnet?tab=readme-ov-file#using-the-openaiclient-class).

## Configuration

The OpenAI resource provides multiple configuration options to meet the requirements and conventions of your project.

### Use automatic parameter handling

The simplest configuration uses automatic parameter handling that looks for the API key in configuration or environment variables:

```csharp
var openai = builder.AddOpenAI("openai", "gpt-4o");
```

The API key will be automatically resolved from:
1. Configuration value at `Parameters:openai-apikey`
2. `OPENAI_API_KEY` environment variable
3. Throws `MissingParameterValueException` if neither is found

Then in user secrets:

```json
{
    "Parameters": 
    {
        "openai-apikey": "YOUR_OPENAI_API_KEY_HERE"
    }
}
```

### Use custom parameters

You can explicitly provide parameters for full control:

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

### Use custom endpoints

The resource supports custom endpoints for OpenAI-compatible services:

```csharp
var endpoint = builder.AddParameter("custom-endpoint", "https://api.custom-openai.com");
var apiKey = builder.AddParameter("custom-key", secret: true);
var model = builder.AddParameter("custom-model", "gpt-4");

var customOpenai = builder.AddOpenAI("custom-openai", endpoint, apiKey, model);
```

### Use fluent configuration

Use fluent methods to customize the resource after creation:

```csharp
var openai = builder.AddOpenAI("openai", "gpt-4")
    .WithModel("gpt-4o")
    .WithEndpoint("https://custom-endpoint.com");
```

## Connection String Format

The OpenAI resource generates connection strings in the following format:

- **With endpoint**: `Endpoint=https://api.example.com;Key=sk-xxx;Model=gpt-4`
- **Without endpoint**: `Key=sk-xxx;Model=gpt-4` (uses default OpenAI endpoint)

These connection strings are compatible with the [Aspire.OpenAI](https://www.nuget.org/packages/Aspire.OpenAI) library and can be consumed directly using `builder.AddOpenAIClient("connectionName")`.

## Available Models

OpenAI supports various AI models. Some popular options include:

- `gpt-4o`
- `gpt-4o-mini`
- `gpt-4`
- `gpt-3.5-turbo`

Check the [OpenAI documentation](https://platform.openai.com/docs/models) for the most up-to-date list of available models.

## Additional documentation

* https://platform.openai.com/docs
* https://github.com/openai/openai-dotnet
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire
