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
