namespace DynamicEcomDZ.Models
{
    public class Products
    {
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        public string? ProductDescription { get; set; }
        public decimal? Quantity { get; set; } = 0;
        public decimal? Rate { get; set; } = 0;
        public decimal? DiscountAmount { get; set; } = 0;
        public string? image { get; set; }
        public decimal? DeliveryCharges { get; set; }
    }
}
