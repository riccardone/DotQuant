using DotQuant.Ui.Components;
using DotQuant.Ui.Services;
using Microsoft.AspNetCore.Components.Authorization;
using NLog;
using NLog.Web;

namespace DotQuant.Ui
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ConfigureLogging();

            var logger = LogManager.GetCurrentClassLogger();
            logger.Info("Application starting...");

            try
            {
                var builder = WebApplication.CreateBuilder(args);
                builder.Services.AddRazorComponents()
                    .AddInteractiveServerComponents();

                builder.Services.AddScoped<MessageSender>();
                builder.Services.AddSingleton<WebSocketClient>();

                var apiUrl = builder.Configuration["ApiEndpoints:DotQuantApi"];
                var webSocketUrl = builder.Configuration["ApiEndpoints:WebSocketUrl"];

                if (string.IsNullOrEmpty(apiUrl))
                    throw new InvalidOperationException("Api URL is not configured in appsettings");
                if (string.IsNullOrEmpty(webSocketUrl))
                    throw new InvalidOperationException("WebSocketUrl URL is not configured in appsettings");
                
                builder.Services.AddHttpClient("Api", client =>
                {
                    client.BaseAddress = new Uri(apiUrl);
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                });
                builder.Services.AddHttpClient("WebSocketUrl", client =>
                {
                    client.BaseAddress = new Uri(webSocketUrl);
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                });
                builder.Services.AddSingleton<AuthService>();

                // Add full authorization support
                builder.Services.AddAuthorization();
                builder.Services.AddAuthorizationCore();
                builder.Services.AddCascadingAuthenticationState();

                // Register authentication provider
                builder.Services.AddSingleton<AuthenticationStateProvider, ApiAuthStateProvider>();

                builder.Logging.ClearProviders();
                builder.Host.UseNLog();

                var app = builder.Build();

                app.UseMiddleware<ExceptionHandlingMiddleware>();

                if (!app.Environment.IsDevelopment())
                {
                    app.UseExceptionHandler("/Error", createScopeForErrors: true);
                    app.UseHsts();
                }

                app.UseHttpsRedirection();

                // Ensure middleware order is correct
                app.UseAuthorization();

                app.UseAntiforgery();
                app.MapStaticAssets();

                app.MapRazorComponents<App>()
                   .AddInteractiveServerRenderMode();

                app.Run();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Application failed to start.");
                throw;
            }
            finally
            {
                LogManager.Shutdown();
            }
        }

        private static void ConfigureLogging()
        {
#if DEBUG
            var nlogConfigPath = Environment.CurrentDirectory + Path.DirectorySeparatorChar + "nlog-dev.config";
#else
            var nlogConfigPath = Environment.CurrentDirectory + Path.DirectorySeparatorChar + "nlog.config";
#endif
            LogManager.Setup()
                .SetupExtensions(e => e.AutoLoadAssemblies(false))
                .LoadConfigurationFromFile(nlogConfigPath, optional: false)
                .LoadConfiguration(builder => builder.LogFactory.AutoShutdown = false)
                .GetCurrentClassLogger();
        }
    }
}
