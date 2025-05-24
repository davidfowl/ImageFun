using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.AppContainers;
using Azure.Provisioning.Expressions;

public static class Extensions
{
    /// <summary>
    /// Fixes endpoint references in the Azure App Service website configuration by resolving environment-specific domains.
    /// </summary>
    /// <param name="projectResource">The project resource builder to configure endpoints for.</param>
    /// <remarks>
    /// This method handles endpoint resolution between different deployment environments:
    /// - If the endpoint reference is within the same environment, it preserves the original configuration
    /// - For cross-environment references (specifically Azure Container Apps), it resolves the correct domain
    /// - Environment variables containing endpoint references are processed and updated accordingly
    /// </remarks>
    /// <exception cref="NotSupportedException">
    /// Thrown when encountering an environment type other than AzureContainerAppEnvironment during endpoint resolution.
    /// </exception>
    public static void FixEndpoints(this IResourceBuilder<ProjectResource> projectResource)
    {
        Dictionary<string, object> env = [];

        projectResource.WithEnvironment(context => env = context.EnvironmentVariables);

        projectResource.PublishAsAzureAppServiceWebsite((infra, website) =>
        {
            foreach (var setting in website.SiteConfig.AppSettings)
            {
                string? name = setting.Value?.Name.Value;

                if (name is null)
                {
                    continue;
                }

                if (env.TryGetValue(name, out var value) && value is EndpointReference e)
                {
                    var thisEnvironment = projectResource.Resource.GetDeploymentTargetAnnotation()?.ComputeEnvironment;
                    var endpointRefEnvironment = e.Resource.GetDeploymentTargetAnnotation()?.ComputeEnvironment;

                    if (thisEnvironment == endpointRefEnvironment)
                    {
                        // This is a reference to the same environment, so we can use the domain
                        // from the environment instead of the one from the project.
                        continue;
                    }

                    // We need to resolve the endpoint from the endpointRefEnvironment
                    // We only support AzureContainerAppEnvironment
                    if (endpointRefEnvironment is AzureContainerAppEnvironmentResource endpointRefEnv)
                    {
                        // Get the domain from the environment. We should expose this as a property
                        // on the AzureContainerAppEnvironmentResource.
                        var domainParameter = new BicepOutputReference("AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN", endpointRefEnv).AsProvisioningParameter(infra);

                        setting!.Value!.Value = BicepFunction.Interpolate(
                            $"{e.Scheme}://{e.Resource.Name}.{domainParameter}"
                        );
                    }
                    else
                    {
                        throw new NotSupportedException($"Unsupported environment type: {endpointRefEnvironment?.GetType()}");
                    }
                }
            }
        });
    }
}