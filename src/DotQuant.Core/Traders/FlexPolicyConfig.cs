using DotQuant.Core.Common;

namespace DotQuant.Core.Traders;

public class FlexPolicyConfig
{
    public decimal OrderPercentage { get; set; } = 0.01m;
    public bool Shorting { get; set; } = false;
    public string PriceType { get; set; } = "DEFAULT";
    public int Fractions { get; set; } = 0;
    public bool OneOrderOnly { get; set; } = true;
    public decimal SafetyMargin { get; set; } = 0.01m;
    public Amount? MinPrice { get; set; } = null;
    public ExitMode ExitStrategy { get; set; } = ExitMode.Recycle;
    public decimal ExitFraction { get; set; } = 0.5m;
    public bool AllowScaleInAfterRecycle { get; set; } = true;
}