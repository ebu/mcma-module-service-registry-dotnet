using Mcma.Api.AspNetCore;
using Mcma.Api.Http;
using Mcma.Api.Routing.Defaults;
using Mcma.Data.MongoDB;
using Mcma.Logging.Serilog;
using Mcma.Model;
using Mcma.Storage.LocalFileSystem;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Mcma.Modules.ServiceRegistry.Kubernetes.ApiHandler
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            LocalFileSystemHelper.AddTypes();

            services.AddMcmaSerilogLogging(loggerConfig => loggerConfig.WriteTo.Console(), "service-registry")
                    .AddMcmaMongoDb()
                    .AddMcmaApi(builder =>
                                    builder.AddDefaultRoutes<Service>()
                                           .AddDefaultRoutes<JobProfile>());
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseMcmaApiHandler();
        }
    }
}
