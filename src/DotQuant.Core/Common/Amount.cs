namespace DotQuant.Core.Common;

public sealed record Amount(Currency Currency, decimal Value)
{
    public override string ToString() => $"{Value:F2} {Currency}";

    public decimal Convert(Currency targetCurrency, DateTime time)
    {
        // TODO: Implement FX conversion logic (stubbed for now)
        if (Currency == targetCurrency) return Value;
        return 0.0m; // Placeholder
    }

    public static Amount operator +(Amount a, Amount b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException("Cannot add amounts with different currencies");

        return new Amount(a.Currency, a.Value + b.Value);
    }

    public static Amount operator -(Amount a, Amount b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException("Cannot subtract amounts with different currencies");

        return new Amount(a.Currency, a.Value - b.Value);
    }

    public static bool operator >(Amount a, Amount b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException("Cannot compare amounts with different currencies");

        return a.Value > b.Value;
    }

    public static bool operator <(Amount a, Amount b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException("Cannot compare amounts with different currencies");

        return a.Value < b.Value;
    }
}