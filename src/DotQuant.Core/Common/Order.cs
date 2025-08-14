namespace DotQuant.Core.Common
{
    public enum TIF
    {
        DAY,
        GTC
    }

    public sealed class Order
    {
        public IAsset Asset { get; }
        public Size Size { get; }
        public decimal Limit { get; }
        public TIF Tif { get; }
        public string Tag { get; }

        public bool Buy => Size.Quantity > Size.Zero.Quantity;
        public bool Sell => Size.Quantity < Size.Zero.Quantity;

        public string Id { get; set; } = string.Empty;
        public Size Fill { get; set; } = Size.Zero;

        public Order(IAsset asset, Size size, decimal limitPrice, TIF tif = TIF.DAY, string tag = "")
        {
            Asset = asset ?? throw new ArgumentNullException(nameof(asset));
            Size = size;
            Limit = limitPrice;
            Tif = tif;
            Tag = tag;
        }

        public Order Cancel()
        {
            if (string.IsNullOrEmpty(Id))
                throw new InvalidOperationException("Cannot cancel an order without a valid ID.");

            return new Order(Asset, Size.Zero, Limit, Tif, Tag) { Id = this.Id, Fill = this.Fill };
        }

        public Order Modify(Size? size = null, decimal? limit = null)
        {
            if (string.IsNullOrEmpty(Id))
                throw new InvalidOperationException("Cannot modify an order without a valid ID.");

            return new Order(
                Asset,
                size ?? this.Size,
                limit ?? this.Limit,
                this.Tif,
                this.Tag
            )
            {
                Id = this.Id,
                Fill = this.Fill
            };
        }

        public bool IsCancellation() => Size.IsZero && !string.IsNullOrEmpty(Id);

        public bool IsExecutable(decimal price) =>
            (Buy && price <= Limit) || (Sell && price >= Limit);

        public bool IsModify() => !Size.IsZero && !string.IsNullOrEmpty(Id);

        public override string ToString() => $"asset={Asset} id={Id} tag={Tag}";
    }
}
