namespace Aspire.Hosting.OpenAI;

/// <summary>
/// Represents an OpenAI resource that encapsulates connection configuration.
/// </summary>
public class OpenAIResource : Resource, IResourceWithConnectionString, IResourceWithoutLifetime
{
    internal ParameterResource? DefaultKeyParameter { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAIResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="model">The model name.</param>
    /// <param name="key">The API key parameter.</param>
    /// <param name="endpoint">The endpoint parameter (optional).</param>
    public OpenAIResource(string name, ReferenceExpression model, ParameterResource key, ReferenceExpression? endpoint = null) : base(name)
    {
        Model = model;
        Key = DefaultKeyParameter = key;
        Endpoint = endpoint;
    }

    /// <summary>
    /// Gets or sets the model name, e.g., "gpt-4o", "gpt-3.5-turbo".
    /// </summary>
    public ReferenceExpression Model { get; set; }

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
    public ReferenceExpression? Endpoint { get; set; }

    /// <summary>
    /// Gets the connection string expression for the OpenAI resource.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        Endpoint is not null
            ? ReferenceExpression.Create($"Endpoint={Endpoint};Key={Key};Model={Model}")
            : ReferenceExpression.Create($"Key={Key};Model={Model}");
}
