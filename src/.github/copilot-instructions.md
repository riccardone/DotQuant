# DotQuant Copilot Instructions

---

# DotQuant Instructions

This file aggregates key setup, architecture, and session history for DotQuant. Update after major changes or sessions to keep the project maintainable and onboarding-friendly.

---

## General Setup & Architecture

- **Dependency Direction:** All projects point to DotQuant.Core; DotQuant.Core does not reference other projects. This ensures plugin and extension projects remain decoupled and maintainable.
- **Plugin/Factory Registration:** All plugin/factory implementations (feeds, brokers, strategies) are registered via Dependency Injection (DI) and discovered using reflection. See `Program.cs` for dynamic plugin registration logic.
- **Configuration Binding:** Factories use configuration binding and DI for construction. Example:
  ```csharp
  var section = config.GetSection("IBKR");
  var ibkrConfig = new IBKRConfig();
  section.Bind(ibkrConfig);
  ```
- **Logging:** Use `ILogger<T>` via DI for structured logs. Avoid `Console.WriteLine` in production code.
- **Service Registration:** Register all factories and core services in DI. Example:
  ```csharp
  services.AddSingleton<IBrokerFactory, IBKRBrokerFactory>();
  services.AddSingleton<IFeedFactory, IbkrFeedFactory>();
  services.AddSingleton<IStrategy, EmaCrossover>();
  ```
- **Program Entrypoint:** Uses factories to resolve and instantiate feeds, brokers, and strategies. See `Program.cs` for selection logic and session orchestration.
- **Supported Targets:** Workspace includes projects for .NET 8, .NET Standard 2.0, .NET Framework 4.8, .NET Core 3.1, .NET 9. Prioritize .NET 8 and .NET 9 for new features.
- **Blazor:** The workspace contains a Blazor project. Prefer Blazor-specific patterns over Razor Pages or MVC when relevant.

---

## complete_dotquant_dataset

### dotquant_alphaVantage_plugin_summary
- **Session Date:** 2025-08-21
- **Goal:** Integrate AiHedgeFund AlphaVantage IDataReader into DotQuant plugin architecture using IFeed and IFeedFactory.
- **Actions Taken:**
  - Analyzed existing IDataReader implementation (AlphaVantageDataReader).
  - Explained IDataReader's methods and use cases (TryGetPrices, TryGetFinancialMetrics, etc.).
  - Mapped IDataReader to a new IFeed implementation: AlphaVantageFeed.
  - Wrapped the IDataReader in a DotQuant-compatible IFeedFactory: AlphaVantageFeedFactory.
  - Provided complete .cs source code file for both AlphaVantageFeed and AlphaVantageFeedFactory.
- **Next Steps:**
  - Build the provided code as a plugin DLL.
  - Drop it into DotQuant's ./plugins directory.
  - Register services like IPriceVolumeProvider and DataFetcher in the main host.
  - Optionally extend IFeed to support fundamentals and sentiment access.
- **Code Provided:**
  - AlphaVantageFeed: Implements IFeed and wraps TryGetPrices into Tick objects.
  - AlphaVantageFeedFactory: Constructs AlphaVantageFeed using DI and plugin conventions.
- **Recommendations:**
  - Add IFeed extensions to support fundamentals or create IFundamentalFeed.
  - Consider using a symbol metadata provider to enrich exchange/currency fields.
  - Optionally register this plugin in DotQuant using service scanning or manual Assembly loading.

---

### dotquant_session_summary_2025_08_22
#### dotquant_host_setup
- **Logging:** Use ILogger<T> via DI for structured logs
- **Configuration:** Supports appsettings.json + environment-specific overrides
- **Http Clients:**
  - AlphaVantage: base_address: https://www.alphavantage.co, auth_handler: AlphaVantageAuthHandler

#### feed_factory_discovery
- **Original:** Relied on plugin DLLs in ./plugins (manual copy required)
- **Issue:** Did not detect referenced projects' IFeedFactory implementations
- **Fix:** Used AppDomain.CurrentDomain.GetAssemblies() to discover all IFeedFactory types in loaded assemblies

#### final_design
- **Factory Registration:** Occurs inside ConfigureServices using reflection over loaded assemblies
- **Plugin Folder:** No longer required unless hybrid support is desired
- **CsvFeedFactory:** Explicitly added to DI as fallback/default

#### program_structure
- **Main:** Builds host, resolves logger, selects feed, runs strategy
- **Worker.Run:** Executes the trading loop
- **PrintAccountSummary:** Logs account state (cash, buying power, positions, open orders)

#### Extras
- **Command Line Args:** --feed, --file, --tickers
- **Safe Reflection:** Handles ReflectionTypeLoadException when scanning types

---

### dotquant_session_summary-23-08-2025
- **Timestamp:** 2025-08-23
- **AlphaVantageFeed:** Inherits LiveFeed, reads historical OHLCV, streams events
- **AlphaVantageFeedFactory:** Key: av, parses tickers, start/end date, resolves services
- **PriceItem:** Fields: Open, High, Low, Close, Volume, TimeSpan
- **Event:** Fields: Time, IReadOnlyList<PriceItem> Items
- **Program.cs Enhancements:** Starting/Final Cash, Net PnL, Buying Power, Open Positions/Orders
- **Logs Analysis:** AAPL: 260 daily records, MSF: No data, PnL: Starting $100,000, Ending ~$98,342

---

### trading212_session_summary
- **Timestamp:** 2025-08-24
- **API Environment:** Demo/live URLs, auth details
- **Working API Call Example:** GET /api/v0/equity/account/cash
- **Deserialization Fix:** Use [JsonPropertyName] attributes
- **Trading212Account Class:** Field mappings
- **Sync Method Logging:** Log on success, fields logged
- **Recommendations:** No API key in query, use proper header, crash on sync failure

---

### dotquant_session_summary
- **Session Date:** 2025-08-14
- **Project:** DotQuant
- **Highlights:** Plugin/factory architecture, CsvFeedFactory, refactored Program.cs, clarified Tick, IBKR attachment options, README.md
- **Design Decisions:** Feed creation via IFeedFactory, built-in vs plugins, async main removed, CSV default path, broker abstraction
- **CLI Contract:** --feed, --file, --tickers
- **Code Patterns:** IFeedFactory interface, program factory usage, csv discovery helper, tick record
- **Next Steps:** Add IBKR plugin, consider SourceLink, semantic versioning, signing, add Worker.RunAsync

---

### aggregated_training_data_full
#### cash_account_conversion_summary
- **Original Kotlin Class:** CashAccount
- **Conversion Goals:** Remove short position logic, use decimal, simplify currency, maintain Amount
- **Final C# Code Summary:** Namespace, class, fields, constructor, method
- **Best Practices:** Avoid double, remove unused logic, use strongly typed money, encapsulate conversion

#### flextrader_debug_session_summary
- **Problem:** FlexTrader not creating orders due to buyingPower = 0
- **Diagnosis:** Margin/buying power logic missing
- **Fix Applied:** Use account.CashAmount().Value
- **Suggestions:** Fallback logic, logging
- **Confirmed Result:** Orders created correctly

#### merged_trading_ai_knowledge
- **Agent Philosophies:** Ben Graham, Charlie Munger, Stanley Druckenmiller, Cathie Wood, Bill Ackman, Warren Buffett
- **Sample Financial Metrics:** Ticker: ACME, Sector: Technology, MarketCap: 52B, Currency: USD, Metrics: PE_Ratio, DebtToEquity, etc.

---

### session_knowledge_summary
#### LiveFeedSubscription
- **Key Points:** Use InteractiveBrokersLiveFeed, subscription methods, auto-currency resolution
- **Currency Handling:** Problem/solution
- **ProgramCS Updates:** Ticker parsing, appsettings, integration
- **PriceItemFix:** Issue/fix
- **NullReferenceFix:** Original/solution
- **IBKRIntegration:** Connection, contract resolution, next step
- **Best Practices:** Avoid hardcoded currency, implement null-safe, use async/await

---

### session_summary
#### Strategy: EmaCrossover
- **FastPeriod:** 12
- **SlowPeriod:** 26
- **Smoothing:** 2.0
- **MinEvents:** 26
- **Signal Logic:** Buy: EmaFast > EmaSlow, Sell: EmaFast < EmaSlow, emits only on change

#### Orders
- **Order 1:** Buy ENI, 68 units @ 14.55
- **Order 2:** Buy ENI, 67 units @ 14.9

#### Position
- **Symbol:** ENI
- **Total Units:** 135
- **Avg Price:** 14.72
- **Market Price:** 14.1
- **UnrealizedPnL:** -84.2

#### Logs
- **Improvements:** Refactored SimBroker, suppressed repetitive EMA signals, cleaner separation

#### Recommendations
- Add SignalType, patch SimBroker, add trade direction handling

---

## Session History

### Session: 2024-09-07

**1. AiHedgeFundProvider Implementation**
- **Why:** To respect dependency direction and avoid circular references, `AiHedgeFundProvider` was moved from `DotQuant.Core` to `DotQuant.Ai.Agents`.
- **How:** Implementation now lives in `DotQuant.Ai.Agents`, referencing interfaces and models from `DotQuant.Core`. The obsolete file in Core was deleted.
- **Impact:** All agent logic is now properly isolated; Core remains the central contract.
- **Code Pattern:**
  ```csharp
  // In DotQuant.Ai.Agents/Services/AiHedgeFundProvider.cs
  public class AiHedgeFundProvider : IAiHedgeFundProvider { ... }
  ```

**2. InteractiveBrokersFeedFactory Fix**
- **Issue:** Factory used a non-existent constructor and blocked on async calls.
- **Fix:** Updated to use `InteractiveBrokersLiveFeed(IOptions<IBKRConfig>)` and bound config from the `IBKR` section. Subscribed tickers asynchronously using fire-and-forget (`_ = feed.ResolveCurrencyAndSubscribe(t)`).
- **Impact:** No more deadlocks or constructor errors; feed is now DI-friendly and robust.
- **Code Pattern:**
  ```csharp
  var options = Options.Create(ibkrConfig);
  var feed = new InteractiveBrokersLiveFeed(options);
  foreach (var t in tickers) _ = feed.ResolveCurrencyAndSubscribe(t);
  ```

**3. IBKRBrokerFactory Implementation**
- **Why:** Needed a factory for IBKRBroker to match DotQuant plugin conventions.
- **How:** Implemented all required interface members (`Key`, `DisplayName`, `Description`, `Create`). Used DI to resolve logger and config.
- **Impact:** IBKRBroker can now be selected and instantiated via DI and command line.
- **Code Pattern:**
  ```csharp
  public IBroker Create(IServiceProvider services, IConfiguration config, ILogger logger) {
      var brokerLogger = services.GetRequiredService<ILogger<IBKRBroker>>();
      return new IBKRBroker(brokerLogger, ibkrConfig);
  }
  ```

**4. Best Practices & Recommendations**
- **Session Summaries:** Maintain a markdown file (this one) to track changes, decisions, and troubleshooting for future reference and onboarding.
- **Plugin Architecture:** Always register new plugins/factories in DI and ensure they follow the dependency direction.
- **Error Handling:** Avoid blocking on async methods in DI contexts; prefer async/await or fire-and-forget for subscriptions.
- **Logging:** Use structured logs for all major operations and errors.
- **Configuration:** Use appsettings.json and environment-specific overrides for all sensitive or environment-dependent settings.

**5. Troubleshooting & Patterns**
- **Common Errors:**
  - Constructor mismatch: Always check the actual constructor signature in the target class.
  - DI resolution: Use `GetRequiredService<T>()` and ensure the correct using directive (`Microsoft.Extensions.DependencyInjection`).
  - Async deadlocks: Never use `.GetAwaiter().GetResult()` in DI or UI code; prefer async patterns.
- **Dynamic Plugin Registration:**
  - Use reflection to discover and register all plugin types at startup.
  - Example:
    ```csharp
    foreach (var type in assemblies.SelectMany(GetLoadableTypes)) {
        if (typeof(IFeedFactory).IsAssignableFrom(type))
            services.AddSingleton(typeof(IFeedFactory), type);
    }
    ```

---

## Next Steps
- Continue updating this file after each major change or session.
- Use this summary for onboarding, troubleshooting, and as a persistent record of architectural decisions.
- Consider adding a troubleshooting section for common errors and their fixes.
- Document new plugin/factory patterns as they are added.

---

## Additional Files
- **AgentPhilosophies.txt:** Contains philosophies for Ben Graham, Charlie Munger, Stanley Druckenmiller, Cathie Wood, Bill Ackman, Warren Buffett.
- **SampleFinancialMetrics.json:** Example metrics for ACME.
- **TrainingExamples.jsonl:** Example trading assistant conversations.

---

*Note: This Markdown conversion preserves the structure and key information from the JSON, making it easier to read and reference for development and code modification purposes.*
