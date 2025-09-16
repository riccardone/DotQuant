using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DotQuant;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Add controllers from DotQuant.Api
        services.AddControllers()
            .AddApplicationPart(typeof(Api.Controllers.SessionController).Assembly)
            .AddControllersAsServices();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}