using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.AppContainers;
using Azure.Provisioning;
using Azure.Provisioning.Expressions;

public static class Extensions
{
    public static void FixEndpoints(this IResourceBuilder<ProjectResource> projectResource)
    {
        Dictionary<string, object> env = [];

        projectResource.WithEnvironment(context =>
        {
            env = context.EnvironmentVariables;
        });

        projectResource.PublishAsAzureAppServiceWebsite((infra, website) =>
        {
            foreach (var setting in website.SiteConfig.AppSettings)
            {
                IBicepValue? v = setting?.Value?.Name;

                if (v == null)
                {
                    continue;
                }

                var name = v.LiteralValue?.ToString();

                if (name == null)
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