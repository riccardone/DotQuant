using DotQuant.Core;
using DotQuant.Core.Brokers;
using DotQuant.Core.Common;
using DotQuant.Core.Feeds;
using DotQuant.Core.Strategies;
using DotQuant.Feeds.AlphaVantage;
using DotQuant.Feeds.AlphaVantage.AlphaVantage;
using DotQuant.Feeds.Csv;
using DotQuant.Brokers.Trading212;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace DotQuant;

internal class Program
{
    public static void Main(string[] args)
    {
        using var host = CreateHostBuilder(args).Build();

        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        var config = host.Services.GetRequiredService<IConfiguration>();
        var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();

        // Optional CLI: --list-brokers
        if (args.Contains("--list-brokers", StringComparer.OrdinalIgnoreCase))
        {
            var registry = host.Services.GetRequiredService<IBrokerFactoryRegistry>();
            Console.WriteLine("Available brokers:");
            foreach (var factory in registry.All)
                Console.WriteLine($"- {factory.Key}: {factory.DisplayName} — {factory.Description}");
            return;
        }

        logger.LogInformation("Starting DotQuant session...");

        var feedType = ParseArg(args, "--feed")?.ToLower() ?? "csv";
        var csvFile = ParseArg(args, "--file");
        var tickersArg = ParseArg(args, "--tickers");
        var strategyName = ParseArg(args, "--strategy");
        var brokerName = ParseArg(args, "--broker");

        logger.LogInformation("Feed selected: {Feed}", feedType);

        // Load Strategy
        var strategies = host.Services.GetServices<IStrategy>().ToList();
        var strategy = SelectPlugin(strategies, strategyName, logger);

        // Load Broker via Factory Registry
        var brokerRegistry = host.Services.GetRequiredService<IBrokerFactoryRegistry>();
        var brokerFactory = !string.IsNullOrWhiteSpace(brokerName)
            ? brokerRegistry.Get(brokerName)
            : brokerRegistry.All.FirstOrDefault(a => a.Key.Equals("sim"));

        if (brokerFactory == null)
            throw new InvalidOperationException("No valid broker factory found.");

        logger.LogInformation("Broker selected: {Broker}", brokerFactory.Key);

        var brokerLogger = loggerFactory.CreateLogger<IBroker>();
        var broker = brokerFactory.Create(host.Services, config, brokerLogger);

        // Load Feed
        var feedFactories = host.Services.GetServices<IFeedFactory>().ToList();
        var feedFactory = feedFactories.FirstOrDefault(f =>
            string.Equals(f.Key, feedType, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"Invalid feed type: {feedType}. Available: {string.Join(", ", feedFactories.Select(f => f.Key))}");

        var argsMap = new Dictionary<string, string?>
        {
            ["--file"] = csvFile,
            ["--tickers"] = tickersArg
        };

        var feed = feedFactory.Create(host.Services, config, logger, argsMap);

        // Run session
        var account = Worker.Run(loggerFactory, feed, strategy, broker);
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
        {
            logger.LogInformation("  - {Symbol}: {Size} units @ {AvgPrice} (Market: {MarketPrice})",
                asset.Symbol, pos.Size.Quantity, pos.AveragePrice, pos.MarketPrice);
        }

        if (account.Orders.Any())
        {
            logger.LogInformation("Open Orders:");
            foreach (var order in account.Orders)
            {
                logger.LogInformation("  - {Side} {Size} {Symbol} (TIF: {Tif})",
                    order.Buy ? "BUY" : order.Sell ? "SELL" : "FLAT",
                    order.Size.Quantity, order.Asset.Symbol, order.Tif);
            }
        }
    }

    private static string? ParseArg(string[] args, string key)
    {
        var idx = Array.IndexOf(args, key);
        return idx >= 0 && idx < args.Length - 1 ? args[idx + 1] : null;
    }

    private static T SelectPlugin<T>(List<T> plugins, string? name, ILogger logger)
    {
        if (!plugins.Any())
            throw new InvalidOperationException($"No implementations of {typeof(T).Name} found.");

        if (string.IsNullOrWhiteSpace(name))
        {
            logger.LogWarning("No --{0} specified, using first discovered: {1}", typeof(T).Name.ToLower(), plugins.First()!.GetType().Name);
            return plugins.First();
        }

        var match = plugins.FirstOrDefault(p => p.GetType().Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (match != null) return match;

        throw new InvalidOperationException($"No plugin named '{name}' found for {typeof(T).Name}. Available: {string.Join(", ", plugins.Select(p => p.GetType().Name))}");
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

                // Core components
                services.AddSingleton<Worker>();
                services.AddSingleton<IPriceVolumeProvider, FakePriceVolumeProvider>();
                services.AddSingleton<DataFetcher>();
                services.AddSingleton<IDataReader, AlphaVantageDataReader>();

                // Built-in strategies/brokers/feeds
                services.AddSingleton<IStrategy, EmaCrossover>();
                services.AddSingleton<IBrokerFactory, Trading212BrokerFactory>();
                services.AddSingleton<IBrokerFactory, SimBrokerFactory>();
                services.AddSingleton<IBrokerFactoryRegistry, BrokerFactoryRegistry>();
                services.AddSingleton<IFeedFactory, CsvFeedFactory>();
                services.AddSingleton<IFeedFactory, AlphaVantageFeedFactory>();

                RegisterDynamicPlugins(services);
            });

    private static void RegisterDynamicPlugins(IServiceCollection services)
    {
        var existing = services
            .Where(d => d.ImplementationType != null)
            .Select(d => d.ImplementationType!)
            .ToHashSet();

        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location));

        foreach (var type in assemblies.SelectMany(GetLoadableTypes))
        {
            if (type == null || type.IsAbstract || type.IsInterface || existing.Contains(type))
                continue;

            if (typeof(IFeedFactory).IsAssignableFrom(type))
                services.AddSingleton(typeof(IFeedFactory), type);

            if (typeof(IStrategy).IsAssignableFrom(type))
                services.AddSingleton(typeof(IStrategy), type);

            if (typeof(IBroker).IsAssignableFrom(type))
                services.AddSingleton(typeof(IBroker), type);
        }
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try { return assembly.GetTypes(); }
        catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null)!; }
    }
}
