namespace DynamicEcomDZ.Models
{
    public class OrderRequest
    {
        public string? ContactNo { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerAddress { get; set; }
        public string? Street { get; set; }
        public string? Floor { get; set; }
        public string? Description { get; set; }
        public List<Products> Items { get; set; }
        public int RestaurantId { get; set; }
    }
}
