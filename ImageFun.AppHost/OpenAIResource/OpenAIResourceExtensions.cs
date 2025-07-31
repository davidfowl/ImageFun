using Aspire.Hosting.OpenAI;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding OpenAI resources to the application model.
/// </summary>
public static class OpenAIResourceExtensions
{
    /// <summary>
    /// Adds an OpenAI resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="model">The model name to use with OpenAI.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<OpenAIResource> AddOpenAI(this IDistributedApplicationBuilder builder, [ResourceName] string name, string model)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(model);

        var defaultApiKeyParameter = builder.AddParameter($"{name}-apikey", () =>
            builder.Configuration[$"Parameters:{name}-apikey"] ??
            Environment.GetEnvironmentVariable("OPENAI_API_KEY") ??
            throw new MissingParameterValueException($"OpenAI API key parameter '{name}-apikey' is missing and OPENAI_API_KEY environment variable is not set."),
            secret: true);
        defaultApiKeyParameter.Resource.Description = """
            The API key used to authenticate requests to the OpenAI API.
            You can create an API key at https://platform.openai.com/api-keys.
            """;
        defaultApiKeyParameter.Resource.EnableDescriptionMarkdown = true;
        var resource = new OpenAIResource(name, ReferenceExpression.Create($"{model}"), defaultApiKeyParameter.Resource);
        resource.DefaultKeyParameter = defaultApiKeyParameter.Resource;

        defaultApiKeyParameter.WithParentRelationship(resource);

        return builder.AddResource(resource)
            .WithInitialState(new()
            {
                ResourceType = "OpenAI",
                CreationTimeStamp = DateTime.UtcNow,
                State = KnownResourceStates.Waiting,
                Properties =
                [
                    new(CustomResourceKnownProperties.Source, "OpenAI")
                ]
            })
            .OnInitializeResource(async (r, evt, ct) =>
            {
                // Connection string resolution is dependent on parameters being resolved
                // We use this to wait for the parameters to be resolved before we can compute the connection string.
                var cs = await r.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false);

                // Publish the update with the connection string value and the state as running.
                // This will allow health checks to start running.
                await evt.Notifications.PublishUpdateAsync(r, s => s with
                {
                    State = KnownResourceStates.Running,
                    Properties = [.. s.Properties, new(CustomResourceKnownProperties.ConnectionString, cs) { IsSensitive = true }]
                }).ConfigureAwait(false);

                // Publish the connection string available event for other resources that may depend on this resource.
                await evt.Eventing.PublishAsync(new ConnectionStringAvailableEvent(r, evt.Services), ct)
                                  .ConfigureAwait(false);
            });
    }

    /// <summary>
    /// Adds an OpenAI resource configured with the standard OpenAI service.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The name of the OpenAI resource.</param>
    /// <param name="apiKey">The API key parameter.</param>
    /// <param name="model">The model parameter.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<OpenAIResource> AddOpenAI(this IDistributedApplicationBuilder builder, [ResourceName] string name, IResourceBuilder<ParameterResource> apiKey, IResourceBuilder<ParameterResource> model)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var resource = new OpenAIResource(name, ReferenceExpression.Create($"{model.Resource}"), apiKey.Resource);
        return builder.AddResource(resource);
    }

    /// <summary>
    /// Adds an OpenAI resource configured with a custom endpoint.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The name of the OpenAI resource.</param>
    /// <param name="endpoint">The endpoint parameter.</param>
    /// <param name="apiKey">The API key parameter.</param>
    /// <param name="model">The model parameter.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<OpenAIResource> AddOpenAI(this IDistributedApplicationBuilder builder, [ResourceName] string name, IResourceBuilder<ParameterResource> endpoint, IResourceBuilder<ParameterResource> apiKey, IResourceBuilder<ParameterResource> model)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var resource = new OpenAIResource(name, ReferenceExpression.Create($"{model.Resource}"), apiKey.Resource, ReferenceExpression.Create($"{endpoint.Resource}"));
        return builder.AddResource(resource);
    }

    /// <summary>
    /// Configures the API key for the OpenAI resource from a parameter.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="apiKey">The API key parameter.</param>
    /// <returns>The resource builder.</returns>
    /// <exception cref="ArgumentException">Thrown when the provided parameter is not marked as secret.</exception>
    public static IResourceBuilder<OpenAIResource> WithApiKey(this IResourceBuilder<OpenAIResource> builder, IResourceBuilder<ParameterResource> apiKey)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(apiKey);

        if (!apiKey.Resource.Secret)
        {
            throw new ArgumentException("The API key parameter must be marked as secret. Use AddParameter with secret: true when creating the parameter.", nameof(apiKey));
        }

        // Remove the existing parameter if it's the default one we created
        if (builder.Resource.DefaultKeyParameter is not null && builder.Resource.DefaultKeyParameter == builder.Resource.Key)
        {
            builder.ApplicationBuilder.Resources.Remove(builder.Resource.DefaultKeyParameter);
            builder.Resource.DefaultKeyParameter = null;
        }

        builder.Resource.Key = apiKey.Resource;

        return builder;
    }

    /// <summary>
    /// Configures the OpenAI resource with a different model.
    /// </summary>
    /// <param name="builder">The OpenAI resource builder.</param>
    /// <param name="model">The model to use.</param>
    /// <returns>The resource builder.</returns>
    public static IResourceBuilder<OpenAIResource> WithModel(this IResourceBuilder<OpenAIResource> builder, string model)
    {
        builder.Resource.Model = ReferenceExpression.Create($"{model}");
        return builder;
    }

    /// <summary>
    /// Configures the OpenAI resource with a custom endpoint.
    /// </summary>
    /// <param name="builder">The OpenAI resource builder.</param>
    /// <param name="endpoint">The endpoint parameter.</param>
    /// <returns>The resource builder.</returns>
    public static IResourceBuilder<OpenAIResource> WithEndpoint(this IResourceBuilder<OpenAIResource> builder, IResourceBuilder<ParameterResource> endpoint)
    {
        builder.Resource.Endpoint = ReferenceExpression.Create($"{endpoint.Resource}");
        return builder;
    }

    /// <summary>
    /// Configures the OpenAI resource with a custom endpoint URL.
    /// </summary>
    /// <param name="builder">The OpenAI resource builder.</param>
    /// <param name="endpoint">The endpoint URL.</param>
    /// <returns>The resource builder.</returns>
    public static IResourceBuilder<OpenAIResource> WithEndpoint(this IResourceBuilder<OpenAIResource> builder, string endpoint)
    {
        builder.Resource.Endpoint = ReferenceExpression.Create($"{endpoint}");
        return builder;
    }
}
