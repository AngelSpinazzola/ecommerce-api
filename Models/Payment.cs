namespace EcommerceAPI.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public virtual Order Order { get; set; } = null!;

        public string? MercadoPagoId { get; set; }
        public string? PreferenceId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = "pending"; // pending, approved, rejected

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; 

        public string? PayerEmail { get; set; }
        public string? PaymentTypeId { get; set; }
        public string? StatusDetail { get; set; } // ← Opcional pero útil
    }
}