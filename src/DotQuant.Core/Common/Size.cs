namespace DotQuant.Core.Common;

public readonly struct Size
{
    public decimal Quantity { get; }

    public Size(decimal quantity)
    {
        Quantity = quantity;
    }

    public bool IsZero => Quantity == 0.0m;
    public bool IsNegative => Quantity < 0.0m;

    public static Size Zero => new Size(0.0m);
    public static Size One => new Size(1.0m);

    public decimal ToDecimal() => Quantity;
}