using DotQuant.Api.Services;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.OpenApi.Models;

namespace DotQuant.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Register services
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddSingleton<ISessionGraphProvider, InMemorySessionGraphProvider>();

        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "DotQuant API",
                Version = "v1",
                Description = "API for session graph data visualization"
            });
        });

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();

            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Swagger UI: https://localhost:7024/swagger");
            logger.LogInformation("OpenAPI JSON: https://localhost:7024/swagger/v1/swagger.json");
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}