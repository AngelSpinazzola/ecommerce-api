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
        public string CustomerPhone { get; set; }

        [StringLength(500)]
        public string CustomerAddress { get; set; }

        // Información de la orden
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Total { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "pending"; // pending, completed, cancelled

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Relación opcional con usuario (si está logueado)
        public int? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        // Relación con items de la orden
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
