namespace DotQuant.Core.Common;

public class Wallet
{
    private readonly List<Amount> _amounts = new();

    public Wallet() { }

    public Wallet(Amount amount)
    {
        Deposit(amount);
    }

    public IReadOnlyList<Amount> Amounts => _amounts.AsReadOnly();

    public IEnumerable<Currency> Currencies => _amounts.Select(a => a.Currency).Distinct();

    public void Deposit(Amount amount)
    {
        if (amount.Value == 0) return;

        var existing = _amounts.FirstOrDefault(a => a.Currency == amount.Currency);
        if (existing != null)
        {
            _amounts.Remove(existing);
            _amounts.Add(new Amount(amount.Currency, existing.Value + amount.Value));
        }
        else
        {
            _amounts.Add(amount);
        }
    }

    public void Deposit(Wallet wallet)
    {
        foreach (var amount in wallet.Amounts)
        {
            Deposit(amount);
        }
    }

    public void Withdraw(Amount amount)
    {
        if (amount.Value == 0) return;

        var existing = _amounts.FirstOrDefault(a => a.Currency == amount.Currency);
        if (existing == null || existing.Value < amount.Value)
        {
            throw new InvalidOperationException($"Insufficient funds in {amount.Currency.Code}.");
        }

        _amounts.Remove(existing);
        _amounts.Add(new Amount(amount.Currency, existing.Value - amount.Value));
    }

    /// <summary>
    /// Returns the total value converted to the specified currency.
    /// Requires external FX pricing to be realistic.
    /// </summary>
    public Amount Total(Currency targetCurrency, DateTimeOffset time)
    {
        // ⚠️ Stub: In a real system, you'd query a price feed
        decimal total = 0m;
        foreach (var amt in _amounts)
        {
            if (amt.Currency == targetCurrency)
            {
                total += amt.Value;
            }
            else
            {
                // Example: assume 1:1 for now — replace with real FX
                total += amt.Value;
            }
        }
        return new Amount(targetCurrency, total);
    }

    public static Wallet operator +(Wallet a, Wallet b)
    {
        var result = new Wallet(new Amount(Currency.EUR, 0.0m)); // Neutral init

        foreach (var amt in a._amounts.Concat(b._amounts))
        {
            result.Deposit(amt);
        }

        return result;
    }

    public override string ToString()
    {
        return string.Join(", ", _amounts.Select(a => $"{a.Value} {a.Currency.Code}"));
    }

    public void Clear()
    {
        _amounts.Clear();
    }
}
