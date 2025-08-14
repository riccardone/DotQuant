using System.Runtime.Loader;
using DotQuant.Core;
using DotQuant.Core.Common;
using DotQuant.Core.Feeds;
using DotQuant.Core.Strategies;
using DotQuant.FeedFromCsv;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DotQuant;

internal class Program
{
    public static void Main(string[] args)
    {
        using var host = CreateHostBuilder(args).Build();

        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Starting DotQuant session...");

        // Parse CLI
        var feedType = ParseArg(args, "--feed")?.ToLower();
        var csvFile = ParseArg(args, "--file");
        var tickersArg = ParseArg(args, "--tickers");

        if (string.IsNullOrWhiteSpace(feedType))
        {
            feedType = "csv";
            logger.LogInformation("No feed specified, defaulting to CSV.");
        }

        var strategy = host.Services.GetRequiredService<EmaCrossover>();
        var factories = host.Services.GetServices<IFeedFactory>().ToList();

        if (factories.Count == 0)
            throw new InvalidOperationException("No feed factories registered.");

        var factory = factories.FirstOrDefault(f => string.Equals(f.Key, feedType, StringComparison.OrdinalIgnoreCase));
        if (factory is null)
            throw new ArgumentException($"Invalid feed type: {feedType}. Available: {string.Join(", ", factories.Select(f => f.Key))}");

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

    private static string? ParseArg(string[] args, string key)
    {
        var idx = Array.IndexOf(args, key);
        return idx >= 0 && idx < args.Length - 1 ? args[idx + 1] : null;
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
                logging.AddSimpleConsole(o => { o.SingleLine = true; o.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff "; });
                logging.SetMinimumLevel(LogLevel.Information);
                logging.AddFilter("Microsoft.*", LogLevel.Error);
                logging.AddFilter("System.Net.Http.*", LogLevel.Error);
            })
            .ConfigureServices((ctx, services) =>
            {
                services.AddSingleton<EmaCrossover>();
                services.AddSingleton<Worker>();

                // Register factories (first-party + plugins) WITHOUT building a provider
                RegisterFeedFactories(services);
            });

    private static void RegisterFeedFactories(IServiceCollection services)
    {
        // 1) Built-in
        services.AddSingleton<IFeedFactory, CsvFeedFactory>();

        // 2) Discover plugins in ./plugins
        var pluginDir = Path.Combine(AppContext.BaseDirectory, "plugins");
        if (!Directory.Exists(pluginDir)) return;

        foreach (var dll in Directory.EnumerateFiles(pluginDir, "*.dll"))
        {
            try
            {
                var asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(dll);
                var types = asm.GetTypes()
                    .Where(t => !t.IsAbstract && typeof(IFeedFactory).IsAssignableFrom(t));

                foreach (var t in types)
                    services.AddSingleton(typeof(IFeedFactory), t);
            }
            catch
            {
                // Swallow here to avoid DI failures at startup; plugins can log after host builds if desired.
            }
        }
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
}
