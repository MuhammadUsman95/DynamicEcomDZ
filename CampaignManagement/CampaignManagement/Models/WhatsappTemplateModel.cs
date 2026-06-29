namespace CampaignManagement.Models
{
    public class WhatsappTemplateModel
    {
        public int? TemplateId { get; set; }
        public string? Template { get; set; }
        public bool IsActive { get; set; }
    }

    public class ResultResponse
    {
        public int StatusId { get; set; }
        public string? MessageCaption { get; set; }
    }
}