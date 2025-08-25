using System.Globalization;
using System.Reflection;
using DotQuant.Brokers.Trading212;
using DotQuant.Core;
using DotQuant.Core.Brokers;
using DotQuant.Core.Common;
using DotQuant.Core.Feeds;
using DotQuant.Core.Services;
using DotQuant.Core.Strategies;
using DotQuant.Feeds.AlphaVantage;
using DotQuant.Feeds.AlphaVantage.AlphaVantage;
using DotQuant.Feeds.Csv;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DotQuant;

internal class Program
{
    public static void Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.CurrentCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.CurrentCulture;

        using var host = CreateHostBuilder(args).Build();

        var logger = host.Services.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Starting DotQuant session...");

        var config = host.Services.GetRequiredService<IConfiguration>();
        var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();

        if (args.Contains("--list-brokers", StringComparer.OrdinalIgnoreCase))
        {
            var registry = host.Services.GetRequiredService<IBrokerFactoryRegistry>();
            logger.LogInformation("Available brokers:");
            foreach (var factory in registry.All)
                logger.LogInformation($"- {factory.Key}: {factory.DisplayName} — {factory.Description}");
            return;
        }

        var argsMap = BuildArgsMap(args);
        var feedType = argsMap.GetValueOrDefault("--feed")?.ToLower() ?? "csv";
        var strategyName = argsMap.GetValueOrDefault("--strategy");
        var brokerName = argsMap.GetValueOrDefault("--broker");

        logger.LogInformation("Feed selected: {Feed}", feedType);

        var strategies = host.Services.GetServices<IStrategy>().ToList();
        var strategy = SelectPlugin(strategies, strategyName, logger);

        var brokerRegistry = host.Services.GetRequiredService<IBrokerFactoryRegistry>();
        var brokerFactory = !string.IsNullOrWhiteSpace(brokerName)
            ? brokerRegistry.Get(brokerName)
            : brokerRegistry.All.FirstOrDefault(a => a.Key.Equals("sim", StringComparison.OrdinalIgnoreCase));

        if (brokerFactory == null)
            throw new InvalidOperationException("No valid broker factory found.");

        logger.LogInformation("Broker selected: {Broker}", brokerFactory.Key);

        var brokerLogger = loggerFactory.CreateLogger<IBroker>();
        var broker = brokerFactory.Create(host.Services, config, brokerLogger);

        var feedFactories = host.Services.GetServices<IFeedFactory>().ToList();
        var feedFactory = feedFactories.FirstOrDefault(f =>
                              string.Equals(f.Key, feedType, StringComparison.OrdinalIgnoreCase))
                          ?? throw new ArgumentException($"Invalid feed type: {feedType}. Available: {string.Join(", ", feedFactories.Select(f => f.Key))}");

        var feed = feedFactory.Create(host.Services, config, logger, argsMap);

        var startingCash = broker.Sync().CashAmount;
        var account = Worker.Run(loggerFactory, feed, strategy, broker);

        PrintAccountSummary(logger, account, startingCash);

        logger.LogInformation("Press Enter to exit...");
        Console.ReadLine();
    }

    private static Dictionary<string, string?> BuildArgsMap(string[] args)
    {
        var map = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.StartsWith("--"))
            {
                var key = arg;
                if (i + 1 >= args.Length || args[i + 1].StartsWith("--"))
                {
                    map[key] = "true";
                }
                else
                {
                    map[key] = args[i + 1];
                    i++;
                }
            }
        }

        return map;
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

    private static void PrintAccountSummary(ILogger logger, IAccount account, Amount startingCash)
    {
        var endingCash = account.CashAmount;
        var pnl = endingCash - startingCash;
        var pnlStr = pnl.Value >= 0 ? $"+{pnl}" : pnl.ToString();

        logger.LogInformation("====== Final Account Summary ======");
        logger.LogInformation("Starting Cash: {StartingCash}", startingCash);
        logger.LogInformation("Final Cash:    {EndingCash}", endingCash);
        logger.LogInformation("Net PnL:       {PnL}", pnlStr);
        logger.LogInformation("Buying Power:  {BuyingPower}", account.BuyingPower);

        if (account.Positions.Any())
        {
            logger.LogInformation("Open Positions:");
            foreach (var (asset, pos) in account.Positions)
            {
                logger.LogInformation("  - {Symbol}: {Size} units @ {AvgPrice} (Market: {MarketPrice})",
                    asset.Symbol, pos.Size.Quantity, pos.AveragePrice, pos.MarketPrice);
            }
        }
        else
        {
            logger.LogInformation("No open positions.");
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
        else
        {
            logger.LogInformation("No open orders.");
        }

        logger.LogInformation("===================================");
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((ctx, config) =>
            {
                var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "dev";
                config.SetBasePath(Directory.GetCurrentDirectory());
                config.AddJsonFile("appsettings.json", optional: true);
                config.AddJsonFile($"appsettings.{env}.json", optional: true);
                config.AddEnvironmentVariables();
                config.AddCommandLine(args);
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
                var configuration = ctx.Configuration;

                services.AddHttpClient();

                var apiKey = configuration["AlphaVantage:ApiKey"];
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    services.AddHttpClient("AlphaVantage", client =>
                    {
                        client.BaseAddress = new Uri("https://www.alphavantage.co");
                    }).AddHttpMessageHandler(() => new AlphaVantageAuthHandler(apiKey));
                }

                services.AddHttpClient<IMarketStatusService, MarketStatusService>();

                services.AddSingleton<Worker>();
                services.AddSingleton<IPriceVolumeProvider, FakePriceVolumeProvider>();
                services.AddSingleton<DataFetcher>();
                services.AddSingleton<IDataReader, AlphaVantageDataReader>();

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
