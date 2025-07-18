namespace EcommerceAPI.DTOs
{
    public class OrderDto
    {
        public int Id { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerAddress { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; }

        // Descripción amigable del estado
        public string StatusDescription { get; set; }

        // Información de pago
        public string? PaymentMethod { get; set; }
        public string? PaymentReceiptUrl { get; set; }
        public DateTime? PaymentReceiptUploadedAt { get; set; }
        public DateTime? PaymentApprovedAt { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }

        // Información de administración
        public string? AdminNotes { get; set; }
        public string? TrackingNumber { get; set; }
        public string? ShippingProvider { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int? UserId { get; set; }
        public List<OrderItemDto> OrderItems { get; set; } = new();
    }
}
