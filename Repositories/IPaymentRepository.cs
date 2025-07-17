using EcommerceAPI.Models;

namespace EcommerceAPI.Repositories
{
    public interface IPaymentRepository
    {
        Task<Payment> CreateAsync(Payment payment);
        Task<Payment?> GetByIdAsync(int id);
        Task<Payment?> GetByOrderIdAsync(int orderId);
        Task<Payment?> GetByMercadoPagoIdAsync(string mercadoPagoId);
        Task<Payment?> GetByPreferenceIdAsync(string preferenceId);
        Task<Payment?> UpdateAsync(int id, Payment payment);
        Task<bool> UpdateStatusAsync(int id, string status, string? statusDetail = null);
    }
}
