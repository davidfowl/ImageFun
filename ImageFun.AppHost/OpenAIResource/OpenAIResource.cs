using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace ImageFun.AppHost.Resources;

/// <summary>
/// Represents an OpenAI resource that encapsulates connection configuration.
/// </summary>
public class OpenAIResource : Resource, IResourceWithConnectionString, IResourceWithoutLifetime
{
    internal ParameterResource DefaultKeyParameter { get; set; }
    internal ParameterResource? DefaultEndpointParameter { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAIResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="model">The model name.</param>
    /// <param name="key">The API key parameter.</param>
    /// <param name="endpoint">The endpoint parameter (optional).</param>
    public OpenAIResource(string name, string model, ParameterResource key, ParameterResource? endpoint = null) : base(name)
    {
        Model = model;
        Key = DefaultKeyParameter = key;
        Endpoint = DefaultEndpointParameter = endpoint;
    }

    /// <summary>
    /// Gets or sets the model name, e.g., "gpt-4o", "gpt-3.5-turbo".
    /// </summary>
    public string Model { get; set; }

    /// <summary>
    /// Gets or sets the API key for accessing the OpenAI service.
    /// </summary>
    public ParameterResource Key { get; set; }

    /// <summary>
    /// Gets or sets the endpoint URL for the OpenAI service.
    /// </summary>
    /// <remarks>
    /// If not set, the default OpenAI endpoint will be used.
    /// </remarks>
    public ParameterResource? Endpoint { get; set; }

    /// <summary>
    /// Gets the connection string expression for the OpenAI resource.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        Endpoint is not null
            ? ReferenceExpression.Create($"Endpoint={Endpoint};Key={Key};Model={Model}")
            : ReferenceExpression.Create($"Key={Key};Model={Model}");
}

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
        var resource = new OpenAIResource(name, model, defaultApiKeyParameter.Resource);

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

        var resource = new OpenAIResource(name, model.Resource.Value, apiKey.Resource);
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

        var resource = new OpenAIResource(name, model.Resource.Value, apiKey.Resource, endpoint.Resource);
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

        // Remove the existing parameter if it's the default one
        if (builder.Resource.DefaultKeyParameter == builder.Resource.Key)
        {
            builder.ApplicationBuilder.Resources.Remove(builder.Resource.Key);
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
        builder.Resource.Model = model;
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
        // Remove the existing endpoint parameter if it's one we created
        if (builder.Resource.DefaultEndpointParameter == builder.Resource.Endpoint && builder.Resource.Endpoint is not null)
        {
            builder.ApplicationBuilder.Resources.Remove(builder.Resource.Endpoint);
        }

        builder.Resource.Endpoint = endpoint.Resource;
        builder.Resource.DefaultEndpointParameter = null; // Clear the default since we're using a custom one

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
        // Remove the existing endpoint parameter if it's one we created
        if (builder.Resource.DefaultEndpointParameter == builder.Resource.Endpoint && builder.Resource.Endpoint is not null)
        {
            builder.ApplicationBuilder.Resources.Remove(builder.Resource.Endpoint);
        }

        var endpointParameter = builder.ApplicationBuilder.AddParameter($"{builder.Resource.Name}-endpoint", endpoint);
        builder.Resource.Endpoint = endpointParameter.Resource;
        builder.Resource.DefaultEndpointParameter = endpointParameter.Resource; // Track this as our created parameter

        return builder;
    }
}
