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

## Session History

- See previous session summaries for details on plugin integration, DI patterns, and troubleshooting.

---

## Best Practices

- **Constructor Injection:** Always use constructor injection for dependencies.
- **No Console.WriteLine:** Use structured logging via `ILogger<T>`.
- **No Circular References:** DotQuant.Core must not reference other projects.
- **Async Patterns:** Use async/await for all asynchronous operations.
- **Plugin Extensibility:** New plugins/factories must be registered via reflection and follow the DI pattern.
- **Error Handling:** Log and handle all errors gracefully; avoid unhandled exceptions in production code.

---

## Troubleshooting

- **DI Resolution Errors:**  
- Ensure all required services are registered.
- Check for missing or misnamed registrations.
- Review constructor signatures for all services.

- **Plugin Discovery:**  
- If a plugin/factory is not available, ensure it is not abstract/interface and is discoverable via reflection.

- **Configuration Issues:**  
- Ensure all required configuration sections are present in `appsettings.json`.
- Validate configuration binding at startup.

---

## Next Steps

- Continue updating this file after each major change or session.
- Use this summary for onboarding, troubleshooting, and as a persistent record of architectural decisions.
- Document new plugin/factory patterns as they are added.

---

# New Session Notes

### 2024-09-07: AiHedgeFundProvider & DI Fixes

- **AiHedgeFundProvider Implementation:**  
  - Moved from DotQuant.Core to DotQuant.Ai.Agents to respect dependency direction.
  - Implementation now lives in `DotQuant.Ai.Agents/Services/AiHedgeFundProvider.cs` and references only interfaces/models from Core.
  - All agent logic is now properly isolated; Core remains the central contract.

- **DI Registration Patterns:**  
  - Always register all dependencies required by a service.  
  - Example: If `PortfolioManager` requires `IAgentRegistry`, register a concrete implementation (e.g., `AgentRegistry`) in DI:
    ```csharp
    builder.Services.AddSingleton<AiHedgeFund.Contracts.IAgentRegistry, AgentRegistry>();
    ```
  - Register both `DotQuant.Api.Contracts.IDataReader` and `AiHedgeFund.Contracts.IDataReader` if both are used in the solution.

- **Plugin/Factory Reflection:**  
  - Use reflection to register all plugin/factory implementations at startup:
    ```csharp
    var assemblies = AppDomain.CurrentDomain.GetAssemblies();
    foreach (var type in assemblies.SelectMany(a => a.GetTypes())) {
        if (typeof(IFeedFactory).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            builder.Services.AddSingleton(typeof(IFeedFactory), type);
        if (typeof(IBrokerFactory).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            builder.Services.AddSingleton(typeof(IBrokerFactory), type);
        // Add more plugin/factory interfaces as needed
    }
    ```
  - This ensures all plugins and factories are available for DI and selection.

- **Error Handling & Troubleshooting:**  
  - If you see errors like "Unable to resolve service for type 'X' while attempting to activate 'Y'", check that all constructor dependencies are registered in DI.
  - Never use `.GetAwaiter().GetResult()` in DI or UI code; prefer async/await patterns.
  - Use `ILogger<T>` for all structured logs and error reporting.

- **Controllers:**  
  - Always inject `ILogger<T>` into controllers for logging.
  - Return appropriate HTTP status codes and log all major operations and errors.

- **Configuration:** 
  - Use `appsettings.json` and environment-specific overrides for all sensitive or environment-dependent settings.
  - Bind configuration sections to POCOs and register them in DI.

---

*This file is maintained by GitHub Copilot and the DotQuant team. Update after each major session or architectural change.*
