namespace EcommerceAPI.DTOs.MercadoPago
{
    public class PaymentInfoDto
    {
        public long Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusDetail { get; set; } = string.Empty;
        public string PaymentTypeId { get; set; } = string.Empty;
        public decimal TransactionAmount { get; set; }
        public DateTime DateCreated { get; set; }
        public string? ExternalReference { get; set; }
        public PaymentPayerDto? Payer { get; set; }
    }
}
