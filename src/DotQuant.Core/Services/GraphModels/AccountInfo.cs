namespace DotQuant.Core.Services.GraphModels;

public record AccountInfo(
    string Currency,
    decimal Cash,
    decimal BuyingPower,
    decimal Equity
);
