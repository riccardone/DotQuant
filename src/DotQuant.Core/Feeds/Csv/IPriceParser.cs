using DotQuant.Core.Common;

namespace DotQuant.Core.Feeds.Csv;

public interface IPriceParser
{
    void Init(string[] header) { /* optional */ }

    PriceItem Parse(string[] line, IAsset asset);
}