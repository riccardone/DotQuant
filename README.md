# DotQuant

DotQuant is an **open-source, event-driven algorithmic trading platform** written in C#.  
It is inspired by [RoboQuant](https://roboquant.org/) (Kotlin) and redesigned with .NET,  
and a pluggable architecture for broker and data feed integration.

## ✨ Features
- **Feed-factory system** — load market data from built-in or external plugins
- **Strategy layer** — add trading strategies
- **Backtesting** from CSV price data
- **Live trading support** (via broker plugins, e.g., Interactive Brokers)

## 📂 Repository Structure
```
DotQuant/
  ├── DotQuant.Core/           # Core interfaces, types, domain logic
  ├── DotQuant.FeedFromCsv/    # Built-in CSV market data feed
  ├── DotQuant.Brokers.IBKR/   # (Optional) IBKR integration
  ├── DotQuant/                # Main app (Program.cs)
  ├── plugins/                 # Optional external feed/broker plugins
  └── data/                    # Sample CSV price data (for backtests)
```

## 🚀 Getting Started

### 1) Prerequisites
- .NET 9 (or later)
- [Optional] Broker API SDK (e.g., IBKR C# API) for live trading
- CSV historical data for backtesting already built in

### 2) Build
```bash
dotnet build
```

### 3) Run with CSV backtest
DotQuant ships with a **built-in `CsvFeedFactory`**.  
If you don’t specify a file, it will automatically pick the **first `.csv` in `data/`**.

```bash
dotnet run --project DotQuant -- --feed csv --file ./data/SPY.csv
```

Or simply:
```bash
dotnet run --project DotQuant
```
*(will use first CSV found in `data/`)*

### 4) Run with plugins
Plugins are `.dll` assemblies implementing `IFeedFactory` or broker interfaces.

1. Place the DLL in `./plugins` (next to the built executable).
2. Run with:
```bash
dotnet run -- --feed plugin-key --tickers AAPL,MSFT
```
*(The `plugin-key` is the `.Key` property of your `IFeedFactory` implementation.)*

## ⚙️ CLI Arguments

| Argument       | Description |
|----------------|-------------|
| `--feed`       | Feed type key (`csv`, `ibkr`, etc.) |
| `--file`       | Path to CSV file (CSV feeds only) |
| `--tickers`    | Comma-separated symbols (live feeds) |

## 📦 Plugin System
DotQuant supports **runtime discovery** of feed & broker plugins from a `plugins/` folder.  
You can develop a new feed/broker in a separate project, compile it, and drop the DLL here — no rebuild of the main app needed.

**Example:**
```csharp
public class MyCustomFeedFactory : IFeedFactory
{
    public string Key => "myfeed";
    public string Name => "My Custom Feed";

    public IFeed Create(IServiceProvider sp, IConfiguration cfg, ILogger logger, IDictionary<string, string?> args)
    {
        // create and return IFeed implementation
    }
}
```

## 🧪 Example Strategies
The repository includes an example **EMA Crossover** strategy (`EmaCrossover.cs`), demonstrating:
- Price series subscription
- Trade signal generation
- Order submission to the account

## 🛠 Extending DotQuant
- **Add a strategy** → create a new class in `DotQuant.Core.Strategies` implementing your logic.
- **Add a feed** → create a new `IFeedFactory` implementation and either:
  - Add to `DotQuant` as a built-in (register in `Program.cs`), or
  - Ship as a plugin DLL.

## 📜 License
MIT License — free for commercial and non-commercial use.

---
