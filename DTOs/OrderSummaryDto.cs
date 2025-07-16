namespace EcommerceAPI.DTOs
{
    public class OrderSummaryDto
    {
        public int Id { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ItemsCount { get; set; }
    }
}
