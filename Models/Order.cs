using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Models
{
    public class Order
    {
        public int Id { get; set; }

        // Información del cliente 
        [Required]
        [StringLength(100)]
        public string CustomerName { get; set; }
        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string CustomerEmail { get; set; }
        [StringLength(20)]
        public string? CustomerPhone { get; set; }
        [StringLength(500)]
        public string? CustomerAddress { get; set; }

        // Información de la orden
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Total { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "pending_payment";

        // Información de pago por transferencia
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "bank_transfer";

        [StringLength(500)]
        public string? PaymentReceiptUrl { get; set; } // URL del comprobante subido

        public DateTime? PaymentReceiptUploadedAt { get; set; }

        public DateTime? PaymentApprovedAt { get; set; }

        public DateTime? ShippedAt { get; set; }

        public DateTime? DeliveredAt { get; set; }

        // Notas del administrador
        [StringLength(1000)]
        public string? AdminNotes { get; set; }

        // Información de envío
        [StringLength(100)]
        public string? TrackingNumber { get; set; }

        [StringLength(50)]
        public string? ShippingProvider { get; set; }

        // Timestamps 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Relaciones 
        public int? UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
