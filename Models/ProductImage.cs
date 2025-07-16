using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.Models
{
    public class ProductImage
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        [StringLength(500)]
        public string ImageUrl { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public bool IsMain { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }
    }
}
