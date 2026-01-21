using Microsoft.AspNetCore.Mvc;

namespace DynamicEcomDZ.Models
{
    public class RedirectionTAB
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public string? FormUrl { get; set; }
        public string? TabIcon { get; set; }
        public bool? IsActive { get; set; }
    }
}

