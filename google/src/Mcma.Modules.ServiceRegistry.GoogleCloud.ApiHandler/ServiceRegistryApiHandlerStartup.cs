using Mcma.Api.Http;
using Mcma.Api.Routing.Defaults;
using Mcma.Functions.Google.ApiHandler;
using Mcma.Model;

namespace Mcma.GoogleCloud.ServiceRegistry.ApiHandler
{
    public class ServiceRegistryApiHandlerStartup : McmaApiHandlerStartup
    {
        public override string ApplicationName => "service-registry-api-handler";

        public override void BuildApi(McmaApiBuilder apiBuilder)
            =>
                apiBuilder.AddDefaultRoutes<Service>()
                          .AddDefaultRoutes<JobProfile>();

    }
}