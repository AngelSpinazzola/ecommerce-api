using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.DTOs
{
    public class UpdateProductImageOrderDto
    {
        [Required]
        public int ImageId { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int DisplayOrder { get; set; }
    }
}
