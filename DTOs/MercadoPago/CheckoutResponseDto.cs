namespace EcommerceAPI.DTOs.MercadoPago
{
    public class CheckoutResponseDto
    {
        public int OrderId { get; set; }
        public string MercadoPagoUrl { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
