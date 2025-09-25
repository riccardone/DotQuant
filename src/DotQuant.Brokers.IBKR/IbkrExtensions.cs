using DotQuant.Core.Common;
using IBApi;

namespace DotQuant.Brokers.IBKR;

public static class IbkrExtensions
{
    public static Contract ToContract(this IAsset asset)
    {
        var contract = new Contract
        {
            Exchange = "SMART",
            Symbol = asset.Symbol,
            Currency = asset.Currency.Code
        };

        if (asset is Stock)
            contract.SecType = "STK";
        else
            throw new InvalidOperationException($"Asset type {asset} is not yet supported");
        
        return contract;
    }
}