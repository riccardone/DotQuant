using DotQuant.Api.Contracts;
using DotQuant.Api.Contracts.Models;
using DotQuant.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using DotQuant.Api.Auth;
using DotQuant.Api.Middleware;
using Finbuckle.MultiTenant;

namespace DotQuant.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure logging to use Microsoft ILogger and single-line format
            builder.Logging.ClearProviders();
            builder.Logging.AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
            });

            var jwtSettings = builder.Configuration.GetSection("JwtSettings");
            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings["Issuer"],
                        ValidAudience = jwtSettings["Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"])),
                        ClockSkew = TimeSpan.Zero // Ensures tokens expire exactly at their set time
                    };
                });

            builder.Services.AddAuthorization();

            // Add services to the container.
            builder.Services.AddControllers();
            builder = ConfigureServices(builder);

            var app = builder.Build();

            // Get logger and log Swagger link if in development
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            if (app.Environment.IsDevelopment())
            {
                var urls = builder.Configuration["ASPNETCORE_URLS"] ?? "http://localhost:5000";
                foreach (var url in urls.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    logger.LogInformation($"Swagger UI available at: {url.TrimEnd('/')}/swagger");
                }
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseMiddleware<ExceptionMiddleware>();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }

        private static WebApplicationBuilder AddSwagger(WebApplicationBuilder builder)
        {
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "DotQuant BaaS API", Version = "v1" });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
            });
            return builder;
        }

        private static WebApplicationBuilder ConfigureServices(WebApplicationBuilder builder)
        {
            var settings = builder.Configuration.Get<AppSettings>();
            builder.Services.AddSingleton(settings);
            builder.Services.AddOpenApi();
            builder.Services.AddEndpointsApiExplorer();
            builder = AddSwagger(builder);
            builder.Services.AddSwaggerGen();
            builder.Services.AddSingleton<IDataReader, FakeDataReader>();
            builder.Services.AddSingleton<IBaasAuthoriser, Authoriser>();
            builder.Services.AddSingleton<ICloudEventsHandler, CloudEventsHandler>();
            builder.Services.AddSingleton<IIdGenerator, IdGenerator>();
            builder.Services.AddSingleton<IResourceLocator, FileLocator>();
            builder.Services.AddSingleton<ISchemaProvider, SchemaProvider>();
            builder.Services.AddSingleton<IResourceElements, ResourceElements>();
            builder.Services.AddSingleton<IPayloadValidator, AltPayloadValidator>();
            builder.Services.AddMultiTenant<PreludeTenantInfo>().WithConfigurationStore();
            builder.Services.AddSingleton<IMessageSenderFactory, MessageSenderFactory>();
            builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            builder.Services.AddHostedService<RateLimitCleanupService>();
            // Logging is configured in Main
            builder.Services.AddControllers();
            return builder;
        }
    }
}
