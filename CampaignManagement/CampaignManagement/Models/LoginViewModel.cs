using System.ComponentModel.DataAnnotations;

namespace CampaignManagement.Models
{
    public class LoginViewModel
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }
}