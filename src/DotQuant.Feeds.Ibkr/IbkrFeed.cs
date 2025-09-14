using DotQuant.Core.Common;
using DotQuant.Core.Feeds;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Threading.Channels;
using IBApi;
using IBApi.protobuf;

namespace DotQuant.Feeds.Ibkr;

public class IbkrFeed : LiveFeed, EWrapper
{
    private readonly ILogger<IbkrFeed> _logger;
    private readonly string[] _symbols;
    private readonly IConfiguration _config;
    private readonly EClientSocket _client;
    private readonly EReaderSignal _signal;
    private readonly ConcurrentDictionary<int, Symbol> _reqIdToSymbol = new();
    private readonly BlockingCollection<Event> _eventQueue = new();
    private CancellationToken _ct;
    private Task? _backgroundTask;
    private int _nextReqId = 1;
    private bool _connected = false;

    private string IbkrHost => _config["Ibkr:Host"] ?? "127.0.0.1";
    private int IbkrPort => int.TryParse(_config["Ibkr:Port"], out var port) ? port : 7497;
    private int IbkrClientId => int.TryParse(_config["Ibkr:ClientId"], out var id) ? id : 1;

    public IbkrFeed(string[] symbols, ILogger<IbkrFeed> logger, IConfiguration config, IMarketStatusService marketStatusService)
    {
        _symbols = symbols;
        _logger = logger;
        _config = config;
        _signal = new EReaderMonitorSignal();
        _client = new EClientSocket(this, _signal);
        EnableMarketStatus(marketStatusService, logger);
    }

    public override async Task PlayAsync(ChannelWriter<Event> channel, CancellationToken ct)
    {
        _ct = ct;
        var connected = await TryConnectAsync();
        if (!connected)
        {
            _logger.LogError("Failed to connect to IBKR TWS. Feed will not subscribe to symbols.");
            return;
        }
        await Task.Delay(1000, ct); // Wait for connection
        await SubscribeToSymbolsAsync();

        // Start background event processing thread (now async)
        _backgroundTask = Task.Run(() => ProcessEventsAsync(channel, ct), ct);

        await Task.Delay(-1, ct); // Keep running until cancelled
    }

    private async Task ProcessEventsAsync(ChannelWriter<Event> channel, CancellationToken ct)
    {
        try
        {
            foreach (var evt in _eventQueue.GetConsumingEnumerable(ct))
            {
                await SendAsync(evt); // Use async event delivery
            }
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown
        }
    }

    private async Task<bool> TryConnectAsync()
    {
        if (_connected) return true;
        try
        {
            _client.eConnect(IbkrHost, IbkrPort, IbkrClientId);
            var reader = new EReader(_client, _signal);
            reader.Start();
            _ = Task.Run(() =>
            {
                while (_client.IsConnected())
                {
                    _signal.waitForSignal();
                    reader.processMsgs();
                }
            });
            await Task.Delay(500);
            if (!_client.IsConnected())
                throw new InvalidOperationException("IBKR client not connected after eConnect.");
            _connected = true;
            _logger.LogInformation("Connected to IBKR TWS");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to IBKR TWS");
            return false;
        }
    }

    private async Task SubscribeToSymbolsAsync()
    {
        foreach (var symbolStr in _symbols)
        {
            var parts = symbolStr.Split('.');
            if (parts.Length != 2) continue;
            var symbol = new Symbol(parts[0], parts[1]);
            if (!await IsMarketOpenAsync(symbol, _ct))
            {
                _logger.LogInformation("Market is closed for {Symbol}, skipping subscription.", symbolStr);
                continue;
            }
            var contract = new IBApi.Contract
            {
                Symbol = parts[0],
                SecType = "STK",
                Exchange = parts[1],
                Currency = "USD"
            };
            int reqId = _nextReqId++;
            _reqIdToSymbol[reqId] = symbol;
            _client.reqMktData(reqId, contract, "", false, false, null);
            _logger.LogInformation("Subscribed to {Symbol}", symbolStr);
        }
    }

    // EWrapper implementation (tickPrice only for now)
    public void tickPrice(int tickerId, int field, double price, TickAttrib attribs)
    {
        if (!_reqIdToSymbol.TryGetValue(tickerId, out var symbol)) return;
        if (field != 4) return; // 4 = LAST_PRICE

        _logger.LogInformation("Received tick: {Symbol} price={Price}", symbol, price);
        var now = DateTime.UtcNow;
        var currency = "USD"; // fallback
        var asset = new Stock(symbol, Currency.GetInstance(currency));
        var evt = new Event(now, new List<PriceItem>
        {
            new PriceItem(asset, (decimal)price, (decimal)price, (decimal)price, (decimal)price, 0, TimeSpan.FromSeconds(1))
        });
        _eventQueue.Add(evt); // Enqueue for background processing
    }

    public void tickSize(int tickerId, int field, decimal size)
    {
        throw new NotImplementedException();
    }

    public void tickString(int tickerId, int field, string value)
    {
        throw new NotImplementedException();
    }

    public void tickGeneric(int tickerId, int field, double value)
    {
        throw new NotImplementedException();
    }

    // --- EWrapper required methods (empty stubs) ---
    public void openOrder(int orderId, IBApi.Contract contract, IBApi.Order order, IBApi.OrderState orderState) { }
    public void completedOrder(IBApi.Contract contract, IBApi.Order order, IBApi.OrderState orderState) { }
    public void orderStatus(int orderId, string status, decimal filled, decimal remaining, double avgFillPrice, long permId, int parentId, double lastFillPrice, int clientId, string whyHeld, double mktCapPrice) { }
    public void updatePortfolio(IBApi.Contract contract, decimal position, double marketPrice, double marketValue, double averageCost, double unrealizedPNL, double realizedPNL, string accountName) { }
    public void tickOptionComputation(int tickerId, int field, int tickAttrib, double impliedVol, double delta, double optPrice, double pvDividend, double gamma, double vega, double theta, double undPrice) { }
    public void updateMktDepth(int tickerId, int position, int operation, int side, double price, decimal size) { }
    public void updateMktDepthL2(int tickerId, int position, string marketMaker, int operation, int side, double price, decimal size, bool isSmartDepth) { }
    public void position(string account, IBApi.Contract contract, decimal pos, double avgCost) { }
    public void realtimeBar(int reqId, long time, double open, double high, double low, double close, decimal volume, decimal wap, int count) { }
    public void positionMulti(int reqId, string account, string modelCode, IBApi.Contract contract, decimal pos, double avgCost) { }
    public void securityDefinitionOptionParameter(int reqId, string exchange, int underlyingConId, string tradingClass, string multiplier, HashSet<string> expirations, HashSet<double> strikes) { }
    public void securityDefinitionOptionParameterEnd(int reqId) { }
    public void pnlSingle(int reqId, decimal pos, double dailyPnL, double unrealizedPnL, double realizedPnL, double value) { }
    public void tickByTickAllLast(int reqId, int tickType, long time, double price, decimal size, TickAttribLast tickAttribLast, string exchange, string specialConditions) { }
    public void tickByTickBidAsk(int reqId, long time, double bidPrice, double askPrice, decimal bidSize, decimal askSize, TickAttribBidAsk tickAttribBidAsk) { }
    public void tickEFP(int tickerId, int tickType, double basisPoints, string formattedBasisPoints, double totalDividends, int holdDays, string futureExpiry, double dividendImpact, double dividendsToExpiry) { }
    public void deltaNeutralValidation(int reqId, IBApi.DeltaNeutralContract deltaNeutralContract) { }
    public void tickSnapshotEnd(int reqId) { }
    public void nextValidId(int orderId) { }
    public void managedAccounts(string accountsList) { }
    public void accountSummary(int reqId, string account, string tag, string value, string currency) { }
    public void accountSummaryEnd(int reqId) { }
    public void bondContractDetails(int reqId, IBApi.ContractDetails contractDetails) { }
    public void updateAccountValue(string key, string value, string currency, string accountName) { }
    public void updateAccountTime(string timeStamp) { }
    public void accountDownloadEnd(string account) { }
    public void openOrderEnd() { }
    public void contractDetails(int reqId, IBApi.ContractDetails contractDetails) { }
    public void contractDetailsEnd(int reqId) { }
    public void execDetails(int reqId, IBApi.Contract contract, IBApi.Execution execution) { }
    public void execDetailsEnd(int reqId) { }
    public void fundamentalData(int reqId, string data) { }
    public void historicalData(int reqId, IBApi.Bar bar) { }
    public void historicalDataUpdate(int reqId, IBApi.Bar bar) { }
    public void historicalDataEnd(int reqId, string start, string end) { }
    public void marketDataType(int reqId, int marketDataType) { }
    public void updateNewsBulletin(int msgId, int msgType, string message, string origExchange) { }
    public void positionEnd() { }
    public void scannerParameters(string xml) { }
    public void scannerData(int reqId, int rank, IBApi.ContractDetails contractDetails, string distance, string benchmark, string projection, string legsStr) { }
    public void scannerDataEnd(int reqId) { }
    public void receiveFA(int faDataType, string faXmlData) { }
    public void verifyMessageAPI(string apiData) { }
    public void verifyCompleted(bool isSuccessful, string errorText) { }
    public void verifyAndAuthMessageAPI(string apiData, string xyzChallenge) { }
    public void verifyAndAuthCompleted(bool isSuccessful, string errorText) { }
    public void displayGroupList(int reqId, string groups) { }
    public void displayGroupUpdated(int reqId, string contractInfo) { }
    public void connectAck() { }
    public void positionMultiEnd(int reqId) { }
    public void accountUpdateMulti(int reqId, string account, string modelCode, string key, string value, string currency) { }
    public void accountUpdateMultiEnd(int reqId) { }
    public void softDollarTiers(int reqId, IBApi.SoftDollarTier[] tiers) { }
    public void familyCodes(IBApi.FamilyCode[] familyCodes) { }
    public void symbolSamples(int reqId, IBApi.ContractDescription[] contractDescriptions) { }
    public void mktDepthExchanges(IBApi.DepthMktDataDescription[] depthMktDataDescriptions) { }
    public void tickNews(int tickerId, long timeStamp, string providerCode, string articleId, string headline, string extraData) { }
    public void smartComponents(int reqId, Dictionary<int, KeyValuePair<string, char>> theMap) { }
    public void tickReqParams(int tickerId, double minTick, string bboExchange, int snapshotPermissions) { }
    public void newsProviders(IBApi.NewsProvider[] newsProviders) { }
    public void newsArticle(int requestId, int articleType, string articleText) { }
    public void historicalNews(int requestId, string time, string providerCode, string articleId, string headline) { }
    public void historicalNewsEnd(int requestId, bool hasMore) { }
    public void headTimestamp(int reqId, string headTimestamp) { }
    public void histogramData(int reqId, IBApi.HistogramEntry[] data) { }
    public void rerouteMktDataReq(int reqId, int conid, string exchange) { }
    public void rerouteMktDepthReq(int reqId, int conid, string exchange) { }
    public void marketRule(int marketRuleId, IBApi.PriceIncrement[] priceIncrements) { }
    public void pnl(int reqId, double dailyPnL, double unrealizedPnL, double realizedPnL) { }
    public void historicalTicks(int reqId, IBApi.HistoricalTick[] ticks, bool done) { }
    public void historicalTicksBidAsk(int reqId, IBApi.HistoricalTickBidAsk[] ticks, bool done) { }
    public void historicalTicksLast(int reqId, IBApi.HistoricalTickLast[] ticks, bool done) { }
    public void tickByTickMidPoint(int reqId, long time, double midPoint) { }
    public void orderBound(long orderId, int apiClientId, int apiOrderId) { }
    public void completedOrdersEnd() { }
    public void error(int id, long time, int code, string msg, string advancedOrderRejectJson)
    {
        // Connection status codes: log as Information
        if (code == 2104 || code == 2106 || code == 2158)
        {
            _logger.LogInformation("IBKR connection status: id={Id} code={Code} msg={Msg}", id, code, msg);
            return;
        }
        // No security definition found: log with symbol context
        if (code == 200)
        {
            if (_reqIdToSymbol.TryGetValue(id, out var symbol))
                _logger.LogError("IBKR error: No security definition for {Symbol} (id={Id} code={Code} msg={Msg})", symbol, id, code, msg);
            else
                _logger.LogError("IBKR error: No security definition (id={Id} code={Code} msg={Msg})", id, code, msg);
            return;
        }
        // All other errors
        _logger.LogError("IBKR error: id={Id} code={Code} msg={Msg} advanced={Advanced}", id, code, msg, advancedOrderRejectJson);
    }
    public void error(Exception e)
    {
        _logger.LogError(e, "IBKR error");
    }
    public void error(string str)
    {
        _logger.LogError("IBKR error: {Msg}", str);
    }
    public void error(int id, int code, string msg)
    {
        _logger.LogError("IBKR error {Code}: {Msg}", code, msg);
    }
    public void currentTime(long time) { }
    // Error and commission stubs
    public void commissionAndFeesReport(CommissionAndFeesReport commissionAndFeesReport) { }
    public void connectionClosed() => _logger.LogWarning("IBKR connection closed");
    // ProtoBuf and new methods
    public void replaceFAEnd(int reqId, string text) { }
    public void wshMetaData(int reqId, string dataJson) { }
    public void wshEventData(int reqId, string dataJson) { }
    public void historicalSchedule(int reqId, string startDateTime, string endDateTime, string timeZone, HistoricalSession[] sessions) { }
    public void userInfo(int reqId, string whiteBrandingId) { }
    public void currentTimeInMillis(long time) { }
    public void orderStatusProtoBuf(OrderStatus orderStatus) { }
    public void openOrderProtoBuf(OpenOrder openOrder) { }
    public void openOrdersEndProtoBuf(OpenOrdersEnd openOrdersEnd) { }
    public void errorProtoBuf(ErrorMessage errorMessage) { }
    public void execDetailsProtoBuf(ExecutionDetails executionDetails) { }
    public void execDetailsEndProtoBuf(ExecutionDetailsEnd executionDetailsEnd) { }
    public void completedOrderProtoBuf(CompletedOrder completedOrder) { }
    public void completedOrdersEndProtoBuf(CompletedOrdersEnd completedOrdersEnd) { }
    public void orderBoundProtoBuf(OrderBound orderBound) { }
    public void contractDataProtoBuf(ContractData contractData) { }
    public void bondContractDataProtoBuf(ContractData contractData) { }
    public void contractDataEndProtoBuf(ContractDataEnd contractDataEnd) { }
    public void tickPriceProtoBuf(TickPrice tickPrice) { }
    public void tickSizeProtoBuf(TickSize tickSize) { }
    public void tickOptionComputationProtoBuf(TickOptionComputation tickOptionComputation) { }
    public void tickGenericProtoBuf(TickGeneric tickGeneric) { }
    public void tickStringProtoBuf(TickString tickString) { }
    public void tickSnapshotEndProtoBuf(TickSnapshotEnd tickSnapshotEnd) { }
    public void updateMarketDepthProtoBuf(MarketDepth marketDepth) { }
    public void updateMarketDepthL2ProtoBuf(MarketDepthL2 marketDepthL2) { }
    public void marketDataTypeProtoBuf(MarketDataType marketDataType) { }
    public void tickReqParamsProtoBuf(TickReqParams tickReqParams) { }
}