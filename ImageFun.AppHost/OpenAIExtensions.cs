public static class OpenAIExtensions
{
    // Add an open AI compatible endpoint
    public static IResourceBuilder<ConnectionStringResource> AddOpenAIConnection(
        this IDistributedApplicationBuilder builder,
        string name,
        IResourceBuilder<ParameterResource> endpoint,
        IResourceBuilder<ParameterResource> key,
        IResourceBuilder<ParameterResource> model)
    {
        return builder.AddConnectionString(name, cs =>
        {
            cs.Append($"Endpoint={endpoint};Key={key};Model={model}");
        });
    }

    public static IResourceBuilder<ConnectionStringResource> AddGithubModelConnection(
        this IDistributedApplicationBuilder builder,
        string name,
        IResourceBuilder<ParameterResource> key,
        IResourceBuilder<ParameterResource> model)
    {
        return builder.AddConnectionString(name, cs =>
        {
            cs.Append($"Endpoint=https://models.github.ai/inference;Key={key};Model={model}");
        });
    }

    public static IResourceBuilder<ConnectionStringResource> AddOpenAIConnection(
        this IDistributedApplicationBuilder builder,
        string name,
        IResourceBuilder<ParameterResource> key,
        IResourceBuilder<ParameterResource> model)
    {
        // Assume the default open AI endpoint
        return builder.AddConnectionString(name, cs =>
        {
            cs.Append($"Key={key};Model={model}");
        });
    }
}