using DotQuant.Core;
using DotQuant.Core.Common;
using DotQuant.Core.Feeds;
using DotQuant.Core.Strategies;
using DotQuant.Feeds.AlphaVantage.AlphaVantage;
using DotQuant.Feeds.Csv;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;
using DotQuant.Feeds.AlphaVantage;

namespace DotQuant;

internal class Program
{
    public static void Main(string[] args)
    {
        using var host = CreateHostBuilder(args).Build();

        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Starting DotQuant session...");

        var feedType = ParseArg(args, "--feed")?.ToLower() ?? "csv";
        var csvFile = ParseArg(args, "--file");
        var tickersArg = ParseArg(args, "--tickers");

        logger.LogInformation("Feed selected: {Feed}", feedType);

        var strategy = host.Services.GetRequiredService<EmaCrossover>();
        var factories = host.Services.GetServices<IFeedFactory>().ToList();

        if (factories.Count == 0)
            throw new InvalidOperationException("No feed factories registered.");

        var factory = factories.FirstOrDefault(f => string.Equals(f.Key, feedType, StringComparison.OrdinalIgnoreCase));
        if (factory is null)
            throw new ArgumentException(
                $"Invalid feed type: {feedType}. Available: {string.Join(", ", factories.Select(f => f.Key))}");

        var argsMap = new Dictionary<string, string?>
        {
            ["--file"] = csvFile,
            ["--tickers"] = tickersArg
        };

        var cfg = host.Services.GetRequiredService<IConfiguration>();
        var feed = factory.Create(host.Services, cfg, logger, argsMap);

        var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
        var account = Worker.Run(loggerFactory, feed, strategy);

        PrintAccountSummary(logger, account);

        logger.LogInformation("Press Enter to exit...");
        Console.ReadLine();
    }

    private static void PrintAccountSummary(ILogger logger, IAccount account)
    {
        logger.LogInformation("Final Account Summary:");
        logger.LogInformation("Cash: {Cash}", account.Cash);
        logger.LogInformation("Buying Power: {BuyingPower}", account.BuyingPower);

        logger.LogInformation("Positions:");
        foreach (var (asset, pos) in account.Positions)
            logger.LogInformation("  - {Symbol}: {Size} units @ {AvgPrice} (Market: {MarketPrice})",
                asset.Symbol, pos.Size.Quantity, pos.AveragePrice, pos.MarketPrice);

        if (account.Orders.Any())
        {
            logger.LogInformation("Open Orders:");
            foreach (var order in account.Orders)
                logger.LogInformation("  - {Side} {Size} {Symbol} (TIF: {Tif})",
                    order.Buy ? "BUY" : order.Sell ? "SELL" : "FLAT",
                    order.Size.Quantity, order.Asset.Symbol, order.Tif);
        }
    }

    private static string? ParseArg(string[] args, string key)
    {
        var idx = Array.IndexOf(args, key);
        return idx >= 0 && idx < args.Length - 1 ? args[idx + 1] : null;
    }

    private static IConfigurationRoot BuildConfig()
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "dev";
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{env}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((ctx, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true)
                      .AddEnvironmentVariables()
                      .AddCommandLine(args);
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
                });
                logging.SetMinimumLevel(LogLevel.Information);
                logging.AddFilter("Microsoft.*", LogLevel.Error);
                logging.AddFilter("System.Net.Http.*", LogLevel.Error);
            })
            .ConfigureServices((ctx, services) =>
            {
                var configuration = BuildConfig();
                services.AddHttpClient();

                var apiKey = configuration["AlphaVantage:ApiKey"];
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    services.AddHttpClient("AlphaVantage",
                            client => { client.BaseAddress = new Uri("https://www.alphavantage.co"); })
                        .AddHttpMessageHandler(() => new AlphaVantageAuthHandler(apiKey));
                }

                services.AddSingleton<EmaCrossover>();
                services.AddSingleton<Worker>();
                services.AddSingleton<IPriceVolumeProvider, FakePriceVolumeProvider>();
                services.AddSingleton<DataFetcher>();

                // Discover IFeedFactory types from all loaded assemblies
                RegisterFeedFactories(services);
            });

    private static void RegisterFeedFactories(IServiceCollection services)
    {
        // Add built-in factory
        services.AddSingleton<IFeedFactory, CsvFeedFactory>();
        services.AddSingleton<IFeedFactory, AlphaVantageFeedFactory>();

        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location));

        var factoryTypes = assemblies
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null)!; }
            })
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                        && typeof(IFeedFactory).IsAssignableFrom(t)
                        && t != typeof(CsvFeedFactory))
            .ToList();

        foreach (var type in factoryTypes)
        {
            services.AddSingleton(typeof(IFeedFactory), type);
        }
    }
}
