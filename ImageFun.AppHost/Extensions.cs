using Aspire.Hosting.Azure.AppContainers;
using Azure.Provisioning;
using Azure.Provisioning.Expressions;

public static class Extensions
{
    // HACK!!
    public static void FixEndpoint(this IResourceBuilder<ProjectResource> projectResource, IResourceBuilder<AzureContainerAppEnvironmentResource> beEnv)
    {
        var domain = beEnv.GetOutput("AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN");

        projectResource.PublishAsAzureAppServiceWebsite((infra, website) =>
        {
            var domainProperty = domain.AsProvisioningParameter(infra);

            var http = BicepFunction.Interpolate($"http://image-processor.{domainProperty}");
            var https = BicepFunction.Interpolate($"https://image-processor.{domainProperty}");

            foreach (var setting in website.SiteConfig.AppSettings)
            {
                IBicepValue? v = setting?.Value?.Name;

                if (v == null)
                    continue;

                if (v.LiteralValue is string s && s == "services__image-processor__http__0")
                {
                    setting!.Value!.Value = http;
                }
                else if (v.LiteralValue is string s2 && s2 == "services__image-processor__https__0")
                {
                    setting!.Value!.Value = https;
                }
            }
        });
    }
}