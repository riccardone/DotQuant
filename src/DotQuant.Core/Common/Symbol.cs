namespace DotQuant.Core.Common;

public sealed record Symbol(string Ticker, string Exchange)
{
    public override string ToString() => $"{Ticker}.{Exchange}";
}