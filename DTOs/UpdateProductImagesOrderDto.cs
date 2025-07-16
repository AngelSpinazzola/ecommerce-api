using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.DTOs
{
    public class UpdateProductImagesOrderDto
    {
        [Required]
        public List<UpdateProductImageOrderDto> Images { get; set; } = new();
    }
}
