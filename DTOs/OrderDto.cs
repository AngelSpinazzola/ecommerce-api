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
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int? UserId { get; set; }
        public List<OrderItemDto> OrderItems { get; set; } = new();
    }
}
