using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.DTOs
{
    public class CreateOrderDto
    {
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

        [Required]
        public List<CreateOrderItemDto> Items { get; set; } = new();
    }
}
