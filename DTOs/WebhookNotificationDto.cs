namespace EcommerceAPI.DTOs
{
    public class WebhookNotificationDto
    {
        public string? Action { get; set; }
        public string? ApiVersion { get; set; }
        public WebhookDataDto? Data { get; set; }
        public DateTime DateCreated { get; set; }
        public long Id { get; set; }
        public bool LiveMode { get; set; }
        public string? Type { get; set; }
        public string? UserId { get; set; }
    }
}
