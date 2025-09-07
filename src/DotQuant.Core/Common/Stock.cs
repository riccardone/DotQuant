namespace DotQuant.Core.Common;

public sealed record Stock(Symbol symbol, Currency currency) : AssetBase(symbol.ToString(), currency)
{
    public override string Serialize() => $"Stock;{symbol.Ticker}.{symbol.Exchange};{currency}";

    public static Stock Deserialize(string value)
    {
        var parts = value.Split(';');
        if (parts.Length != 3 || parts[0] != "Stock")
            throw new FormatException("Invalid Stock serialization format");

        var symbolParts = parts[1].Split('.');
        if (symbolParts.Length != 2)
            throw new FormatException("Invalid Symbol format");

        var symbol = new Symbol(symbolParts[0], symbolParts[1]);
        var currency = Currency.GetInstance(parts[2]);

        return new Stock(symbol, currency);
    }
}