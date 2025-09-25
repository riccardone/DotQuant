using System.Collections.Concurrent;
using System.Globalization;
using DotQuant.Core.Common;
using IBApi;
using Microsoft.Extensions.Logging;

namespace DotQuant.Brokers.IBKR;

public class IBKR
{
    public const int MaxResponseTime = 5000;

    private readonly ILogger<IBKR> _logger;
    private readonly Dictionary<int, EClientSocket> _connections = new();
    private readonly ConcurrentDictionary<int, IAsset> _assetCache = new();

    public IBKR(ILogger<IBKR> logger)
    {
        _logger = logger;
    }

    public void Register(int conId, IAsset asset)
    {
        if (conId > 0)
            _assetCache[conId] = asset;
    }

    public void Disconnect(EClientSocket client)
    {
        try
        {
            if (client.IsConnected())
                client.eDisconnect();
        }
        catch (IOException ex)
        {
            _logger.LogInformation(ex.Message);
        }
    }

    public EClientSocket Connect(EWrapper wrapper, IBKRConfig config)
    {
        if (_connections.TryGetValue(config.Client, out var oldClient))
            Disconnect(oldClient);

        var signal = new EReaderMonitorSignal();
        var client = new EClientSocket(wrapper, signal);

        client.eConnect(config.Host, config.Port, config.Client);

        // Wait briefly to confirm connection
        int attempts = 0;
        while (!client.IsConnected() && attempts < 50)
        {
            Thread.Sleep(100);
            attempts++;
        }

        if (!client.IsConnected())
            throw new InvalidOperationException($"Couldn't connect with config {config}");

        _logger.LogInformation("Connected with config {Config}", config);

        var reader = new EReader(client, signal);
        reader.Start();

        var thread = new Thread(() =>
        {
            while (client.IsConnected())
            {
                signal.waitForSignal();
                try
                {
                    reader.processMsgs();
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Exception handling IBKR message");
                }
            }
        })
        {
            IsBackground = true
        };
        thread.Start();

        _connections[config.Client] = client;
        return client;
    }

    public string GetFormattedTime(DateTime time)
    {
        return time.ToString("yyyyMMdd-HH:mm:ss", CultureInfo.InvariantCulture);
    }

    public IAsset ToAsset(Contract contract)
    {
        if (_assetCache.TryGetValue(contract.ConId, out var result))
            return result;

        IAsset asset = contract.SecType switch
        {
            "STK" => new Stock(new Symbol(contract.Symbol, contract.Exchange), Currency.GetInstance(contract.Currency)),
            _ => throw new InvalidOperationException($"Unsupported asset type {contract.SecType}")
        };

        _assetCache[contract.ConId] = asset;
        return asset;
    }
}