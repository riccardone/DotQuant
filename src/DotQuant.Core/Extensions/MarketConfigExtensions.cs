using DotQuant.Core.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DotQuant.Core.Extensions;

public static class MarketConfigExtensions
{
    public static Currency ResolveCurrency(this IConfiguration config, Symbol symbol, ILogger? logger = null)
    {
        var exchange = symbol.Exchange.ToUpperInvariant();
        var currencyCode = config[$"MarketHours:{exchange}:Currency"];

        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            logger?.LogWarning("No currency found for exchange {Exchange}, defaulting to USD", exchange);
            return Currency.GetInstance("USD");
        }

        return Currency.GetInstance(currencyCode);
    }
}