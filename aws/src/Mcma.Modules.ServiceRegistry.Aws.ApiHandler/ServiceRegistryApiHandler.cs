using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Mcma.Api.Routing.Defaults;
using Mcma.Functions.Aws;
using Mcma.Functions.Aws.ApiHandler;
using Mcma.Model;
using Mcma.Serialization.Aws;
using Microsoft.Extensions.DependencyInjection;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]

namespace Mcma.Modules.ServiceRegistry.Aws.ApiHandler
{
    public class ServiceRegistryApiHandler : McmaLambdaFunction<McmaLambdaApiHandler, APIGatewayProxyRequest, APIGatewayProxyResponse>
    {
        protected override void Configure(IServiceCollection services)
            => services.AddMcmaLambdaApiHandler("service-registry-api-handler",
                                                apiBuilder =>
                                                 apiBuilder.AddDefaultRoutes<Service>()
                                                           .AddDefaultRoutes<JobProfile>());
    }
}
