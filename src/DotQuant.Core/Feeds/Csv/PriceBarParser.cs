using System.Globalization;
using System.Text.RegularExpressions;
using DotQuant.Core.Common;

namespace DotQuant.Core.Feeds.Csv;

public class PriceBarParser : IPriceParser
{
    private int _open = -1;
    private int _high = -1;
    private int _low = -1;
    private int _close = -1;
    private int _volume = -1;
    private int _adjustedClose = -1;
    private int _currency = -1;

    private readonly bool _priceAdjust;
    private readonly bool _autodetect;
    private readonly TimeSpan _timeSpan;

    public PriceBarParser(
        int open = -1, int high = -1, int low = -1, int close = -1, int volume = -1,
        int adjustedClose = -1, int currency = -1, bool priceAdjust = false, bool autodetect = true,
        TimeSpan? timeSpan = null)
    {
        _open = open;
        _high = high;
        _low = low;
        _close = close;
        _volume = volume;
        _adjustedClose = adjustedClose;
        _currency = currency;

        _priceAdjust = priceAdjust;
        _autodetect = autodetect;
        _timeSpan = timeSpan ?? TimeSpan.FromHours(1);
    }

    private void Validate()
    {
        if (_open == -1) throw new InvalidOperationException("No open-prices column");
        if (_low == -1) throw new InvalidOperationException("No low-prices column");
        if (_high == -1) throw new InvalidOperationException("No high-prices column");
        if (_close == -1) throw new InvalidOperationException("No close-prices column");
        if (_priceAdjust && _adjustedClose == -1)
            throw new InvalidOperationException("No adjusted close prices column");
    }

    public void Init(string[] header)
    {
        if (!_autodetect)
        {
            Validate();
            return;
        }

        var regex = new Regex("[^A-Z]");
        for (var i = 0; i < header.Length; i++)
        {
            var clean = regex.Replace(header[i].ToUpperInvariant(), "");
            switch (clean)
            {
                case "OPEN": _open = i; break;
                case "HIGH": _high = i; break;
                case "LOW": _low = i; break;
                case "CLOSE": _close = i; break;
                case "ADJCLOSE":
                case "ADJUSTEDCLOSE": _adjustedClose = i; break;
                case "VOLUME":
                case "VOL": _volume = i; break;
                case "CURRENCY": _currency = i; break;
            }
        }

        Validate();
    }

    public PriceItem Parse(string[] line, IAsset asset)
    {
        var currencyCode = _currency != -1 ? line[_currency].Trim().ToUpperInvariant() : null;
        var currency = currencyCode != null ? Currency.GetInstance(currencyCode) : Currency.USD;

        // Re-wrap the asset with updated currency if it's a Stock
        var enrichedAsset = asset switch
        {
            Stock s => new Stock(s.symbol, currency),
            _ => asset
        };

        var open = decimal.Parse(line[_open], CultureInfo.InvariantCulture);
        var high = decimal.Parse(line[_high], CultureInfo.InvariantCulture);
        var low = decimal.Parse(line[_low], CultureInfo.InvariantCulture);
        var close = decimal.Parse(line[_close], CultureInfo.InvariantCulture);

        var volume = _volume != -1 && !string.IsNullOrWhiteSpace(line[_volume])
            ? decimal.Parse(line[_volume], CultureInfo.InvariantCulture)
            : decimal.Zero;

        var item = new PriceItem(enrichedAsset, open, high, low, close, volume, _timeSpan);

        if (_priceAdjust)
            item.AdjustClose(decimal.Parse(line[_adjustedClose], CultureInfo.InvariantCulture));

        return item;
    }
}
