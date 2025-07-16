using EcommerceAPI.DTOs;

namespace EcommerceAPI.Services
{
    public interface IOrderService
    {
        Task<OrderDto> CreateOrderAsync(CreateOrderDto createOrderDto, int? userId = null);
        Task<OrderDto?> GetOrderByIdAsync(int id);
        Task<IEnumerable<OrderSummaryDto>> GetAllOrdersAsync();
        Task<IEnumerable<OrderSummaryDto>> GetOrdersByUserIdAsync(int userId);
        Task<bool> UpdateOrderStatusAsync(int id, string status);
        Task<IEnumerable<OrderSummaryDto>> GetOrdersByStatusAsync(string status);
    }
}
