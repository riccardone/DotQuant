namespace DotQuant.Core.Common;

/// <summary>
/// AccountModel that models a plain cash account. No additional leverage or margin is available for trading.
/// </summary>
public class CashAccount : IAccountModel
{
    private readonly decimal _minimum;

    /// <summary>
    /// Initializes a new instance of the <see cref="CashAccount"/> class with an optional minimum cash threshold.
    /// </summary>
    /// <param name="minimum">The minimum amount of cash to maintain in the account. Default is 0.0.</param>
    public CashAccount(decimal minimum = 0.0m)
    {
        _minimum = minimum;
    }

    /// <summary>
    /// Update the account with recalculated buying power, deducting short exposure and minimum.
    /// </summary>
    /// <param name="account">The internal account to update.</param>
    public void UpdateAccount(InternalAccount account)
    {
        //var shortExposure = account.Positions.Short.Exposure();
        //var remaining = account.Cash - shortExposure;

        var converted = account.Convert(account.CashAmount);
        account.BuyingPower = new Amount(account.BaseCurrency, converted.Value - _minimum);
    }
}