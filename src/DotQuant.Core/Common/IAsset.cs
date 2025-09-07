using System.Collections.Concurrent;

namespace DotQuant.Core.Common;

public interface IAsset : IComparable<IAsset>
{
    internal const string SEP = ";";
    string Symbol { get; }
    Currency Currency { get; }

    string Serialize();

    Amount Value(Size size, decimal price);
}

public abstract record AssetBase(string Symbol, Currency Currency) : IAsset
{
    public abstract string Serialize();

    public Amount Value(Size size, decimal price)
    {
        return size.IsZero ? new Amount(Currency, 0.0m) : new Amount(Currency, size.ToDecimal() * price);
    }

    public int CompareTo(IAsset? other)
    {
        if (other == null) return 1;
        return string.Compare(Symbol, other.Symbol, StringComparison.Ordinal);
    }
}

public sealed record Option(string Symbol, Currency Currency) : AssetBase(Symbol, Currency)
{
    public override string Serialize() => $"Option;{Symbol};{Currency}";

    public static Option Deserialize(string value)
    {
        var parts = value.Split(';');
        return new Option(parts[0], Currency.GetInstance(parts[1]));
    }
}

public sealed record Crypto(string Symbol, Currency Currency) : AssetBase(Symbol, Currency)
{
    public override string Serialize() => $"Crypto;{Symbol};{Currency}";

    public static Crypto Deserialize(string value)
    {
        var parts = value.Split(';');
        return new Crypto(parts[0], Currency.GetInstance(parts[1]));
    }
}

public sealed record Forex(string Symbol, Currency Currency) : AssetBase(Symbol, Currency)
{
    public override string Serialize() => $"Forex;{Symbol};{Currency}";

    public static Forex FromSymbol(string symbol)
    {
        return new Forex(symbol, Currency.USD); // TODO: allow custom currency
    }
}

public static class AssetFactory
{
    private static readonly ConcurrentDictionary<string, IAsset> Cache = new();

    private static readonly Dictionary<string, Func<string, IAsset>> Registry = new()
    {
        ["Stock"] = s => Stock.Deserialize(s),
        ["Option"] = s => Option.Deserialize(s),
        ["Crypto"] = s => Crypto.Deserialize(s)
        // Add more if needed
    };

    public static IAsset Deserialize(string value)
    {
        return Cache.GetOrAdd(value, v =>
        {
            var sepIndex = v.IndexOf(';');
            if (sepIndex < 0) throw new ArgumentException("Invalid serialized asset format.");

            var type = v[..sepIndex];
            var serString = v[(sepIndex + 1)..];

            if (!Registry.TryGetValue(type, out var deserializer))
                throw new ArgumentException($"Unknown asset type: {type}");

            return deserializer(serString);
        });
    }
}

// Example support classes

public sealed record Currency(string Code)
{
    public static readonly Currency USD = new("USD");
    public static readonly Currency EUR = new("EUR");

    public static Currency GetInstance(string code)
    {
        return new Currency(code.ToUpperInvariant());
    }

    public override string ToString() => Code;
}