using Mcma.Api;
using Mcma.Api.Routing.Defaults.Routes;
using Mcma.AspNetCore;
using Mcma.LocalFileSystem;
using Mcma.MongoDb;
using Mcma.Serilog;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Mcma.Modules.ServiceRegistry.Generic.ApiHandler
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

            var mcmaConfig = Configuration.GetSection("Mcma");

            services.AddMcmaSerilogLogging(mcmaConfig["ServiceName"],
                                           new LoggerConfiguration()
                                               .WriteTo.Console()
                                               .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
                                               .CreateLogger());
            
            services.AddMcmaMongoDb(opts => mcmaConfig.Bind("MongoDb", opts));

            services.AddMcmaApi(builder =>
                                    builder.Configure(opts => opts.PublicUrl = mcmaConfig["PublicUrl"])
                                           .AddDefaultRoutes<Service>()
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
