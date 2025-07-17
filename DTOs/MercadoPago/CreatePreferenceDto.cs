namespace EcommerceAPI.DTOs.MercadoPago
{
    public class CreatePreferenceDto
    {
        public List<PreferenceItemDto> Items { get; set; } = new();
        public string? ExternalReference { get; set; }
        public bool AutoReturn { get; set; } = true;
        public string? NotificationUrl { get; set; }
        public PreferencePayerDto? Payer { get; set; }
    }
}
