using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        [Required]
        [Range(0.01, 999999.99)]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue)]
        public int Stock { get; set; }

        [StringLength(100)]
        public string Category { get; set; }

        // Imagen principal (para compatibilidad y performance)
        [StringLength(500)]
        public string MainImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property - Relación con ProductImages
        public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    }
}
