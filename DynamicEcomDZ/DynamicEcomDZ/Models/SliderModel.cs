using DynamicEcomDZ.Models;

namespace DynamicEcomDZ.Models
{
    public class SliderModel
    {
        public int SilderId { get; set; }
        public string? SilderName { get; set; }
        public int? SliderMovingTimer { get; set; }
        // ✅ NEW FIELDS
        public string? HeadingSlider { get; set; }
        public string? DescriptionSlider { get; set; }
    }
    public class DetailSliderModel
    {
        public int SilderId { get; set; }
        public string? SilderName { get; set; }
        public int SliderMovingTimer { get; set; }
        public string? HeadingSlider { get; set; }
        public string? DescriptionSlider { get; set; }
    }

    public class SliderViewModel
    {
        public List<SliderModel> Sliders { get; set; }
        public List<CustomerModel> Customers { get; set; }

        public SliderViewModel()
        {
            Sliders = new List<SliderModel>();
            Customers = new List<CustomerModel>();
        }
    }

    public class CategoryModel
    {
        public int TypeId { get; set; }
        public string TypeName { get; set; }
        public string Category { get; set; }
        public string CategoryImage { get; set; }
    }

}