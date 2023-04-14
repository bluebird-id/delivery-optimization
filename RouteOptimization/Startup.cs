using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using RouteOptimization.Repositories;
using RouteOptimization.Services;
using RouteOptimization.Protos;

namespace RouteOptimization
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
            //IOC
            services.AddSingleton<Repositories.Client.OsrmClient>();

            services.AddSingleton<IRepository, ORToolsRepository>();
            services.AddScoped<RouteOptimationService.RouteOptimationServiceBase, RoutingSolverService>();

            services.AddGrpc().AddJsonTranscoding();
            services.AddGrpcReflection();
            services.AddControllers();

            services.AddGrpcSwagger();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Bluebird.RouteOptimization.Service", Version = "v1" });

                var filePath = Path.Combine(System.AppContext.BaseDirectory, "RouteOptimization.xml");
                c.IncludeXmlComments(filePath);
                c.IncludeGrpcXmlComments(filePath, includeControllerXmlComments: true);
            });
            services.AddHealthChecks();

            services.Configure<RouteOptions>(options => options.AppendTrailingSlash = false);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("v1/swagger.json", "RouteOptimization.Service v1"));

            app.UseRouting();

            //app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<RoutingSolverService>();

                endpoints.MapGrpcReflectionService(); //  Focus!!!
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });

                endpoints.MapControllers();
                endpoints.MapHealthChecks("/hc", new HealthCheckOptions()
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });

                endpoints.MapGet("/map/route", async context =>
                {
                    context.Response.ContentType = "text/html";
                    await context.Response.SendFileAsync("Views/index.html", 0, null, default);
                });
                endpoints.MapGet("/map/xroute", async context =>
                {
                    context.Response.ContentType = "text/html";
                    await context.Response.SendFileAsync("Views/xindex.html", 0, null, default);
                });
            });
        }
    }
}