using EcommerceAPI.Data;
using EcommerceAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly ApplicationDbContext _context;

        public PaymentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Payment> CreateAsync(Payment payment)
        {
            payment.CreatedAt = DateTime.UtcNow;
            payment.UpdatedAt = DateTime.UtcNow;

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task<Payment?> GetByIdAsync(int id)
        {
            return await _context.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Payment?> GetByOrderIdAsync(int orderId)
        {
            return await _context.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.OrderId == orderId);
        }

        public async Task<Payment?> GetByMercadoPagoIdAsync(string mercadoPagoId)
        {
            return await _context.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.MercadoPagoId == mercadoPagoId);
        }

        public async Task<Payment?> GetByPreferenceIdAsync(string preferenceId)
        {
            return await _context.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.PreferenceId == preferenceId);
        }

        public async Task<Payment?> UpdateAsync(int id, Payment payment)
        {
            var existingPayment = await _context.Payments.FindAsync(id);
            if (existingPayment == null)
                return null;

            existingPayment.MercadoPagoId = payment.MercadoPagoId;
            existingPayment.Status = payment.Status;
            existingPayment.PayerEmail = payment.PayerEmail;
            existingPayment.PaymentTypeId = payment.PaymentTypeId;
            existingPayment.StatusDetail = payment.StatusDetail;
            existingPayment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existingPayment;
        }

        public async Task<bool> UpdateStatusAsync(int id, string status, string? statusDetail = null)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
                return false;

            payment.Status = status;
            payment.StatusDetail = statusDetail;
            payment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
