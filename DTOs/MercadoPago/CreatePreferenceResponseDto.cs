namespace EcommerceAPI.DTOs.MercadoPago
{
    public class CreatePreferenceResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string InitPoint { get; set; } = string.Empty;
        public string SandboxInitPoint { get; set; } = string.Empty;
        public DateTime DateCreated { get; set; }
        public string? ExternalReference { get; set; }
    }
}
