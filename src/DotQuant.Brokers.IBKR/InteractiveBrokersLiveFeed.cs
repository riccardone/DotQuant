using DotQuant.Core.Common;
using DotQuant.Core.Feeds;
using IBApi;
using IBApi.protobuf;
using Microsoft.Extensions.Options;
using Contract = IBApi.Contract;
using ContractDetails = IBApi.ContractDetails;
using DeltaNeutralContract = IBApi.DeltaNeutralContract;
using Execution = IBApi.Execution;
using Order = IBApi.Order;
using OrderState = IBApi.OrderState;
using SoftDollarTier = IBApi.SoftDollarTier;

namespace DotQuant.Brokers.IBKR;

public class InteractiveBrokersLiveFeed : LiveFeed, EWrapper
{
    private readonly EClientSocket _client;
    private readonly EReaderSignal _signal;
    private int _nextRequestId = 1;

    private readonly Dictionary<int, IAsset> _subscriptions = new();
    private readonly Dictionary<int, TaskCompletionSource<ContractDetails>> _contractDetailRequests = new();

    public InteractiveBrokersLiveFeed(IOptions<IBKRConfig> config)
    {
        _signal = new EReaderMonitorSignal();
        var clientImpl = new EClientSocket(this, _signal);
        _client = clientImpl;

        // Connect to TWS or IB Gateway
        _client.eConnect(config.Value.Host, config.Value.Port, config.Value.Client);

        // Start background reader
        var reader = new EReader(_client, _signal);
        reader.Start();
        Task.Run(() =>
        {
            while (_client.IsConnected())
            {
                _signal.waitForSignal();
                reader.processMsgs();
            }
        });
    }

    /// <summary>
    /// Resolves the correct currency from IBKR via reqContractDetails, then subscribes.
    /// </summary>
    public async Task ResolveCurrencyAndSubscribe(string symbol, string exchange = "SMART", string secType = "STK")
    {
        var contract = new Contract
        {
            Symbol = symbol,
            SecType = secType,
            Exchange = exchange,
            Currency = "" // leave blank, IBKR will fill in
        };

        int reqId = _nextRequestId++;
        var tcs = new TaskCompletionSource<ContractDetails>();
        _contractDetailRequests[reqId] = tcs;

        _client.reqContractDetails(reqId, contract);

        // Wait for response (with timeout)
        if (await Task.WhenAny(tcs.Task, Task.Delay(5000)) != tcs.Task)
            throw new TimeoutException($"Timeout resolving contract details for {symbol}");

        var details = await tcs.Task;
        var resolvedCurrency = details.Contract.Currency;
        var resolvedSymbol = details.Contract.Symbol;

        Console.WriteLine($"[IBKR] Resolved {resolvedSymbol} currency: {resolvedCurrency}");
        
        // Now subscribe to market data
        var asset = new Stock(new Symbol(resolvedSymbol, contract.Exchange), new Currency(resolvedCurrency));
        SubscribeRealTimeBars(asset, exchange, resolvedCurrency);
    }

    /// <summary>
    /// Subscribe to real-time market data for a symbol (tick-by-tick last price).
    /// </summary>
    public void SubscribeMarketData(IAsset asset, string exchange = "SMART", string currency = "EUR")
    {
        int reqId = _nextRequestId++;
        _subscriptions[reqId] = asset;

        var contract = new Contract
        {
            Symbol = asset.Symbol,
            SecType = "STK",
            Exchange = exchange,     // "SMART" auto-routes
            Currency = currency      // e.g., EUR for European stocks
        };

        _client.reqMktData(reqId, contract, string.Empty, false, false, null);
    }

    /// <summary>
    /// Standard subscription with known currency.
    /// </summary>
    public void SubscribeRealTimeBars(IAsset asset, string exchange = "SMART", string currency = "USD")
    {
        int reqId = _nextRequestId++;
        _subscriptions[reqId] = asset;

        var contract = new Contract
        {
            Symbol = asset.Symbol,
            SecType = "STK",
            Exchange = exchange,
            Currency = currency
        };

        _client.reqRealTimeBars(reqId, contract, 5, "TRADES", false, null);
    }

    // --- EWrapper Callbacks ---

    //public void tickPrice(int tickerId, int field, double price, TickAttrib attribs)
    //{
    //    if (!_subscriptions.ContainsKey(tickerId) || price <= 0) return;
    //    var asset = _subscriptions[tickerId];

    //    var priceItem = new PriceItem(asset, DateTimeOffset.UtcNow, (decimal)price);
    //    Send(new Event(DateTimeOffset.UtcNow, new List<PriceItem> { priceItem }));
    //}

    public void tickPrice(int tickerId, int field, double price, TickAttrib attribs)
    {
        if (!_subscriptions.ContainsKey(tickerId) || price <= 0) return;

        var asset = _subscriptions[tickerId];
        var now = DateTimeOffset.UtcNow;

        // Use price for Open/High/Low/Close (tick-by-tick doesn't have OHLC)
        var priceItem = new PriceItem(
            asset,
            (decimal)price,   // Open
            (decimal)price,   // High
            (decimal)price,   // Low
            (decimal)price,   // Close
            0m,               // Volume (not available in tickPrice)
            TimeSpan.Zero     // Instantaneous tick
        );

        SendAsync(new Event(now, new List<PriceItem> { priceItem })).GetAwaiter().GetResult();
    }

    public void tickSize(int tickerId, int field, decimal size)
    {
        throw new NotImplementedException();
    }

    //public void realtimeBar(int reqId, long time, double open, double high, double low, double close,
    //    long volume, double wap, int count)
    //{
    //    if (!_subscriptions.ContainsKey(reqId)) return;
    //    var asset = _subscriptions[reqId];

    //    var priceItem = new PriceItem(asset, DateTimeOffset.FromUnixTimeSeconds(time),
    //        (decimal)open, (decimal)high, (decimal)low, (decimal)close, volume);
    //    Send(new Event(priceItem.Time, new List<PriceItem> { priceItem }));
    //}

    // Required empty EWrapper implementations
    public void error(Exception e) => Console.WriteLine($"[IBKR ERROR] {e}");
    public void error(string str) => Console.WriteLine($"[IBKR ERROR] {str}");
    public void error(int id, long errorTime, int errorCode, string errorMsg, string advancedOrderRejectJson)
    {
        //throw new NotImplementedException();
    }

    public void error(int id, int code, string msg) => Console.WriteLine($"[IBKR ERROR] {id} {code}: {msg}");
    public void nextValidId(int orderId) { }
    public void connectionClosed() => Console.WriteLine("[IBKR] Connection closed.");
    public void currentTime(long time) { }
    public void tickSize(int tickerId, int field, int size) { }
    public void tickString(int tickerId, int field, string value) { }
    public void tickGeneric(int tickerId, int field, double value) { }

    public void tickEFP(int tickerId, int tickType, double basisPoints, string formattedBasisPoints, double impliedFuture,
        int holdDays, string futureLastTradeDate, double dividendImpact, double dividendsToLastTradeDate)
    {
        throw new NotImplementedException();
    }

    public void deltaNeutralValidation(int reqId, DeltaNeutralContract deltaNeutralContract)
    {
        throw new NotImplementedException();
    }

    public void tickOptionComputation(int tickerId, int field, int tickAttrib, double impliedVolatility, double delta,
        double optPrice, double pvDividend, double gamma, double vega, double theta, double undPrice)
    {
        throw new NotImplementedException();
    }

    public void tickSnapshotEnd(int tickerId) { }
    public void marketDataType(int reqId, int marketDataType) { }
    public void updateMktDepth(int tickerId, int position, int operation, int side, double price, decimal size)
    {
        throw new NotImplementedException();
    }

    public void updateMktDepthL2(int tickerId, int position, string marketMaker, int operation, int side, double price,
        decimal size, bool isSmartDepth)
    {
        throw new NotImplementedException();
    }

    public void updateNewsBulletin(int msgId, int msgType, string message, string origExchange)
    {
        throw new NotImplementedException();
    }

    public void position(string account, Contract contract, decimal pos, double avgCost)
    {
        throw new NotImplementedException();
    }

    public void smartComponents(int reqId, Dictionary<int, KeyValuePair<string, char>> theMap)
    {
        throw new NotImplementedException();
    }

    public void tickReqParams(int tickerId, double minTick, string bboExchange, int snapshotPermissions) { }
    public void newsProviders(NewsProvider[] newsProviders)
    {
        throw new NotImplementedException();
    }

    public void newsArticle(int requestId, int articleType, string articleText)
    {
        throw new NotImplementedException();
    }

    //public void orderStatus(string orderId, string status, double filled, double remaining, double avgFillPrice, int permId,
    //    int parentId, double lastFillPrice, int clientId, string whyHeld, double mktCapPrice)
    //{ }
    //public void openOrder(int orderId, Contract contract, Order order, OrderState orderState) { }
    //public void openOrder(int orderId, Contract contract, Order order, OrderState orderState)
    //{
    //    throw new NotImplementedException();
    //}

    public void openOrder(int orderId, Contract contract, Order order, OrderState orderState)
    {
        throw new NotImplementedException();
    }

    public void openOrderEnd() { }
    public void contractDetails(int reqId, ContractDetails contractDetails)
    {
        if (_contractDetailRequests.TryGetValue(reqId, out var tcs))
        {
            tcs.TrySetResult(contractDetails);
        }
    }

    public void contractDetailsEnd(int reqId)
    {
        _contractDetailRequests.Remove(reqId);
    }

    public void execDetails(int reqId, Contract contract, Execution execution) { }
    public void execDetailsEnd(int reqId) { }
    public void commissionAndFeesReport(CommissionAndFeesReport commissionAndFeesReport)
    {
        throw new NotImplementedException();
    }

    //public void commissionReport(CommissionReport commissionReport) { }
    //public void position(string account, Contract contract, double pos, double avgCost) { }
    public void positionEnd() { }

    public void realtimeBar(int reqId, long time, double open, double high, double low, double close,
        decimal volume, decimal wap, int count)
    {
        if (!_subscriptions.ContainsKey(reqId)) return;

        var asset = _subscriptions[reqId];
        var barTime = DateTimeOffset.FromUnixTimeSeconds(time);

        var priceItem = new PriceItem(
            asset,
            (decimal)open,
            (decimal)high,
            (decimal)low,
            (decimal)close,
            (decimal)volume,
            TimeSpan.FromSeconds(5)  // IBKR real-time bars are fixed 5-second intervals
        );

        SendAsync(new Event(barTime, new List<PriceItem> { priceItem })).GetAwaiter().GetResult();
    }

    public void scannerParameters(string xml)
    {
        throw new NotImplementedException();
    }

    public void scannerData(int reqId, int rank, ContractDetails contractDetails, string distance, string benchmark,
        string projection, string legsStr)
    {
        throw new NotImplementedException();
    }

    public void scannerDataEnd(int reqId)
    {
        throw new NotImplementedException();
    }

    public void receiveFA(int faDataType, string faXmlData)
    {
        throw new NotImplementedException();
    }

    public void verifyMessageAPI(string apiData)
    {
        throw new NotImplementedException();
    }

    public void verifyCompleted(bool isSuccessful, string errorText)
    {
        throw new NotImplementedException();
    }

    public void verifyAndAuthMessageAPI(string apiData, string xyzChallenge)
    {
        throw new NotImplementedException();
    }

    public void verifyAndAuthCompleted(bool isSuccessful, string errorText)
    {
        throw new NotImplementedException();
    }

    public void displayGroupList(int reqId, string groups)
    {
        throw new NotImplementedException();
    }

    public void displayGroupUpdated(int reqId, string contractInfo)
    {
        throw new NotImplementedException();
    }

    public void connectAck()
    {
        throw new NotImplementedException();
    }

    public void positionMulti(int requestId, string account, string modelCode, Contract contract, decimal pos, double avgCost)
    {
        throw new NotImplementedException();
    }

    public void positionMultiEnd(int requestId)
    {
        throw new NotImplementedException();
    }

    public void accountUpdateMulti(int requestId, string account, string modelCode, string key, string value, string currency)
    {
        throw new NotImplementedException();
    }

    public void accountUpdateMultiEnd(int requestId)
    {
        throw new NotImplementedException();
    }

    public void accountSummary(int reqId, string account, string tag, string value, string currency) { }
    public void accountSummaryEnd(int reqId) { }
    public void bondContractDetails(int reqId, ContractDetails contract)
    {
        throw new NotImplementedException();
    }

    public void updateAccountValue(string key, string value, string currency, string accountName)
    {
        throw new NotImplementedException();
    }

    public void updatePortfolio(Contract contract, decimal position, double marketPrice, double marketValue, double averageCost,
        double unrealizedPNL, double realizedPNL, string accountName)
    {
        throw new NotImplementedException();
    }

    public void updateAccountTime(string timestamp)
    {
        throw new NotImplementedException();
    }

    public void managedAccounts(string accountsList) { }
    public void accountDownloadEnd(string account) { }

    public void orderStatus(int orderId, string status, decimal filled, decimal remaining, double avgFillPrice, long permId,
        int parentId, double lastFillPrice, int clientId, string whyHeld, double mktCapPrice)
    {
        throw new NotImplementedException();
    }

    public void fundamentalData(int reqId, string data) { }
    public void historicalData(int reqId, Bar bar) { }
    public void historicalDataUpdate(int reqId, Bar bar)
    {
        throw new NotImplementedException();
    }

    public void historicalDataEnd(int reqId, string start, string end) { }
    public void historicalTicks(int reqId, List<HistoricalTick> ticks, bool done) { }
    public void historicalTicksBidAsk(int reqId, List<HistoricalTickBidAsk> ticks, bool done) { }
    public void historicalTicksLast(int reqId, List<HistoricalTickLast> ticks, bool done) { }
    public void marketRule(int marketRuleId, List<PriceIncrement> priceIncrements) { }
    public void marketRule(int marketRuleId, PriceIncrement[] priceIncrements)
    {
        throw new NotImplementedException();
    }

    public void pnl(int reqId, double dailyPnL, double unrealizedPnL, double realizedPnL) { }
    public void pnlSingle(int reqId, decimal pos, double dailyPnL, double unrealizedPnL, double realizedPnL, double value)
    {
        throw new NotImplementedException();
    }

    public void historicalTicks(int reqId, HistoricalTick[] ticks, bool done)
    {
        throw new NotImplementedException();
    }

    public void historicalTicksBidAsk(int reqId, HistoricalTickBidAsk[] ticks, bool done)
    {
        throw new NotImplementedException();
    }

    public void historicalTicksLast(int reqId, HistoricalTickLast[] ticks, bool done)
    {
        throw new NotImplementedException();
    }

    public void tickByTickAllLast(int reqId, int tickType, long time, double price, decimal size, TickAttribLast tickAttribLast,
        string exchange, string specialConditions)
    {
        throw new NotImplementedException();
    }

    public void tickByTickBidAsk(int reqId, long time, double bidPrice, double askPrice, decimal bidSize, decimal askSize,
        TickAttribBidAsk tickAttribBidAsk)
    {
        throw new NotImplementedException();
    }

    public void pnlSingle(int reqId, int pos, double dailyPnL, double unrealizedPnL, double realizedPnL, double value) { }
    public void newsBulletin(int msgId, int msgType, string message, string origExchange) { }
    public void newsProviders(List<NewsProvider> newsProviders) { }
    public void newsArticle(int requestId, int articleType, int articleSize, string article) { }
    public void historicalNews(int requestId, string time, string providerCode, string articleId, string headline) { }
    public void historicalNewsEnd(int requestId, bool hasMore) { }
    public void headTimestamp(int reqId, string headTimestamp)
    {
        throw new NotImplementedException();
    }

    public void histogramData(int reqId, HistogramEntry[] data)
    {
        throw new NotImplementedException();
    }

    public void rerouteMktDataReq(int reqId, int conId, string exchange)
    {
        throw new NotImplementedException();
    }

    public void rerouteMktDepthReq(int reqId, int conId, string exchange)
    {
        throw new NotImplementedException();
    }

    public void securityDefinitionOptionParameter(int reqId, string exchange, int underlyingConId, string tradingClass,
        string multiplier, HashSet<string> expirations, HashSet<double> strikes)
    { }
    public void securityDefinitionOptionParameterEnd(int reqId) { }
    public void softDollarTiers(int reqId, SoftDollarTier[] tiers) { }
    public void familyCodes(FamilyCode[] familyCodes) { }
    public void symbolSamples(int reqId, ContractDescription[] contractDescriptions)
    {
        throw new NotImplementedException();
    }

    public void mktDepthExchanges(DepthMktDataDescription[] depthMktDataDescriptions)
    {
        throw new NotImplementedException();
    }

    public void tickNews(int tickerId, long timeStamp, string providerCode, string articleId, string headline, string extraData)
    {
        throw new NotImplementedException();
    }

    public void symbolSamples(int reqId, List<ContractDescription> contractDescriptions) { }
    public void mktDepthExchanges(List<DepthMktDataDescription> depthMktDataDescriptions) { }
    public void tickByTickAllLast(int reqId, int tickType, long time, double price, int size,
        TickAttribLast tickAttribLast, string exchange, string specialConditions)
    { }
    public void tickByTickBidAsk(int reqId, long time, double bidPrice, double askPrice, int bidSize, int askSize,
        TickAttribBidAsk tickAttribBidAsk)
    { }
    public void tickByTickMidPoint(int reqId, long time, double midPoint) { }
    public void orderBound(long permId, int clientId, int orderId)
    {
        throw new NotImplementedException();
    }

    public void completedOrder(Contract contract, Order order, OrderState orderState)
    {
        throw new NotImplementedException();
    }

    public void completedOrdersEnd()
    {
        throw new NotImplementedException();
    }

    public void replaceFAEnd(int reqId, string text)
    {
        throw new NotImplementedException();
    }

    public void wshMetaData(int reqId, string dataJson)
    {
        throw new NotImplementedException();
    }

    public void wshEventData(int reqId, string dataJson)
    {
        throw new NotImplementedException();
    }

    public void historicalSchedule(int reqId, string startDateTime, string endDateTime, string timeZone,
        HistoricalSession[] sessions)
    {
        throw new NotImplementedException();
    }

    public void userInfo(int reqId, string whiteBrandingId)
    {
        throw new NotImplementedException();
    }

    public void currentTimeInMillis(long timeInMillis)
    {
        throw new NotImplementedException();
    }

    public void orderStatusProtoBuf(OrderStatus orderStatusProto)
    {
        throw new NotImplementedException();
    }

    public void openOrderProtoBuf(OpenOrder openOrderProto)
    {
        throw new NotImplementedException();
    }

    public void openOrdersEndProtoBuf(OpenOrdersEnd openOrdersEndProto)
    {
        throw new NotImplementedException();
    }

    public void errorProtoBuf(ErrorMessage errorMessageProto)
    {
        throw new NotImplementedException();
    }

    public void execDetailsProtoBuf(ExecutionDetails executionDetailsProto)
    {
        throw new NotImplementedException();
    }

    public void execDetailsEndProtoBuf(ExecutionDetailsEnd executionDetailsEndProto)
    {
        throw new NotImplementedException();
    }

    public void completedOrderProtoBuf(CompletedOrder completedOrderProto)
    {
        throw new NotImplementedException();
    }

    public void completedOrdersEndProtoBuf(CompletedOrdersEnd completedOrdersEndProto)
    {
        throw new NotImplementedException();
    }

    public void orderBoundProtoBuf(OrderBound orderBoundProto)
    {
        throw new NotImplementedException();
    }

    public void contractDataProtoBuf(ContractData contractDataProto)
    {
        throw new NotImplementedException();
    }

    public void bondContractDataProtoBuf(ContractData contractDataProto)
    {
        throw new NotImplementedException();
    }

    public void contractDataEndProtoBuf(ContractDataEnd contractDataEndProto)
    {
        throw new NotImplementedException();
    }

    public void tickPriceProtoBuf(TickPrice tickPriceProto)
    {
        throw new NotImplementedException();
    }

    public void tickSizeProtoBuf(TickSize tickSizeProto)
    {
        throw new NotImplementedException();
    }

    public void tickOptionComputationProtoBuf(TickOptionComputation tickOptionComputationProto)
    {
        throw new NotImplementedException();
    }

    public void tickGenericProtoBuf(TickGeneric tickGenericProto)
    {
        throw new NotImplementedException();
    }

    public void tickStringProtoBuf(TickString tickStringProto)
    {
        throw new NotImplementedException();
    }

    public void tickSnapshotEndProtoBuf(TickSnapshotEnd tickSnapshotEndProto)
    {
        throw new NotImplementedException();
    }

    public void updateMarketDepthProtoBuf(MarketDepth marketDepthProto)
    {
        throw new NotImplementedException();
    }

    public void updateMarketDepthL2ProtoBuf(MarketDepthL2 marketDepthL2Proto)
    {
        throw new NotImplementedException();
    }

    public void marketDataTypeProtoBuf(MarketDataType marketDataTypeProto)
    {
        throw new NotImplementedException();
    }

    public void tickReqParamsProtoBuf(TickReqParams tickReqParamsProto)
    {
        throw new NotImplementedException();
    }
}