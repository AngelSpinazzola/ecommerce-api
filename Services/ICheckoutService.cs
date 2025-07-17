using EcommerceAPI.DTOs;
using EcommerceAPI.DTOs.MercadoPago;

namespace EcommerceAPI.Services
{
    public interface ICheckoutService
    {
        Task<CheckoutResponseDto> CreateCheckoutAsync(CreateOrderDto createOrderDto, int? userId = null);
        Task<bool> ProcessPaymentWebhookAsync(string paymentId, string status, string statusDetail);
        Task<OrderDto?> GetOrderWithPaymentAsync(int orderId);
    }
}
