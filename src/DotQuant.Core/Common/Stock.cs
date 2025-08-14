namespace DotQuant.Core.Common;

public sealed record Stock(string Symbol, Currency Currency) : AssetBase(Symbol, Currency)
{
    public override string Serialize() => $"Stock;{Symbol};{Currency}";

    public static Stock Deserialize(string value)
    {
        var parts = value.Split(';');
        if (parts.Length != 2)
            throw new FormatException("Invalid Stock serialization format");

        return new Stock(parts[0], Currency.GetInstance(parts[1]));
    }
}