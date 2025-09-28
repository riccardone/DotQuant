using System.Net.Http.Headers;
using AiHedgeFund.Agents;
using AiHedgeFund.Agents.Registry;
using AiHedgeFund.Agents.Services;
using AiHedgeFund.Contracts;
using DotQuant.Api.Auth;
using DotQuant.Api.Code;
using DotQuant.Api.Contracts;
using DotQuant.Api.Contracts.Models;
using DotQuant.Api.Middleware;
using DotQuant.Api.Services;
using DotQuant.Core.Brokers;
using DotQuant.Core.Feeds;
using DotQuant.Core.Services;
using Finbuckle.MultiTenant;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using AiHedgeFund.Data;
using AiHedgeFund.Data.AlphaVantage;
using IDataReader = DotQuant.Api.Contracts.IDataReader;

namespace DotQuant.Api;

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
        // Configuration binding
        var settings = builder.Configuration.Get<AppSettings>();
        builder.Services.AddSingleton(settings);
        builder.Services.AddOpenApi();
        builder.Services.AddEndpointsApiExplorer();
        builder = AddSwagger(builder);
        builder.Services.AddSwaggerGen();

        // Register both IDataReader interfaces if needed
        builder.Services.AddSingleton<IDataReader, FakeApiDataReader>();
        builder.Services.AddSingleton<AiHedgeFund.Contracts.IDataReader, AlphaVantageDataReader>();
        builder.Services.AddSingleton<AiHedgeFund.Contracts.IPriceVolumeProvider, AiHedgeFund.Data.Mock.FakePriceVolumeProvider>();
        builder.Services.AddSingleton<DataFetcher>();

        var alphaVantageApiKey = settings?.AlphaVantage.ApiKey;
        if (string.IsNullOrWhiteSpace(alphaVantageApiKey))
            throw new InvalidOperationException("AlphaVantage API key is missing in configuration.");

        builder.Services.AddHttpClient("AlphaVantage", client =>
            {
                client.BaseAddress = new Uri("https://www.alphavantage.co");
            })
            .AddHttpMessageHandler(() => new AlphaVantageAuthHandler(alphaVantageApiKey));
        builder.Services.AddHttpClient("OpenAI", client =>
        {
            var apiKey = settings?.OpenAi.ApiKey;
            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("OpenAI API key is missing in configuration.");
            client.BaseAddress = new Uri("https://api.openai.com/v1/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        });

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
        builder.Services.AddSingleton<ISessionGraphProvider, InMemorySessionGraphProvider>();

        // Register IAgentRegistry for PortfolioManager dependency
        builder.Services.AddSingleton<IAgentRegistry, AgentRegistry>();

        // Register HTTP client factory for OpenAiHttp and agent dependencies
        builder.Services.AddHttpClient();

        // Register AiHedgeFundProvider and its dependencies
        builder.Services.AddSingleton<TradingInitializer>();
        builder.Services.AddSingleton<PortfolioManager>();
        builder.Services.AddSingleton<RiskManagerAgent>();
        builder.Services.AddSingleton<IAiHedgeFundProvider, AiHedgeFundProvider>();

        builder.Services.AddSingleton<IAgentRegistry, AgentRegistry>();
        builder.Services.AddSingleton<BenGrahamAgent>();
        builder.Services.AddSingleton<CathieWoodAgent>();
        builder.Services.AddSingleton<BillAckmanAgent>();
        builder.Services.AddSingleton<CharlieMungerAgent>();
        builder.Services.AddSingleton<StanleyDruckenmillerAgent>();
        builder.Services.AddSingleton<WarrenBuffettAgent>();
        builder.Services.AddSingleton<RiskManagerAgent>();
        builder.Services.AddSingleton<IHttpLib, OpenAiHttp>();

        // Only call AddControllers once
        builder.Services.AddControllers();

        // Configuration binding for AppArguments
        var appArgs = builder.Configuration.GetSection("AppArguments").Get<AppArguments>();
        builder.Services.AddSingleton(appArgs);

        // Plugin/factory registration via reflection (if needed)
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var type in assemblies.SelectMany(a => a.GetTypes()))
        {
            if (typeof(IFeedFactory).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                builder.Services.AddSingleton(typeof(IFeedFactory), type);
            if (typeof(IBrokerFactory).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                builder.Services.AddSingleton(typeof(IBrokerFactory), type);
            // Add more plugin/factory interfaces as needed
        }

        return builder;
    }
}