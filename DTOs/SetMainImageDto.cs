using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.DTOs
{
    public class SetMainImageDto
    {
        [Required]
        public int ImageId { get; set; }
    }
}
