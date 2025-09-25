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

        // Add CORS to allow Blazor UI requests
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins("https://localhost:7140")
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseCors(); // Enable CORS before endpoints
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}