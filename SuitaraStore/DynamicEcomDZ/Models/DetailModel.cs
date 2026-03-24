using DynamicEcomDZ.Models;

namespace DynamicEcomDZ.Models
{
    public class DetailModel
    {
        public string? Category { get; set; }
        public string? ProductName { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductDescription { get; set; }
        public string? ProductImage { get; set; }
        public decimal? Prices { get; set; }
        public decimal? DiscountAmount { get; set; }
        public string? CustomerName { get; set; }
        public decimal? DeliveryCharges { get; set; }
    }
    public class RestaurantDetailViewModel
    {
        public List<DetailSliderModel> Sliders { get; set; } = new();
        public Dictionary<string, List<DetailModel>> Products { get; set; } = new();
        public string? RestaurantName { get; set; } = "Restaurant";
        public int? RestaurantId { get; set; }
        public string? SubHeaderTitle { get; set; }
        public string? RestaurantLogo { get; set; } = "";
        public string? RestaurantAddress { get; set; } = "";


    }

}
