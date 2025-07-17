using EcommerceAPI.DTOs.MercadoPago;

namespace EcommerceAPI.Services
{
    public interface IMercadoPagoService
    {
        Task<CreatePreferenceResponseDto> CreatePreferenceAsync(CreatePreferenceDto preferenceDto);
        Task<PaymentInfoDto> GetPaymentInfoAsync(string paymentId);
        Task<bool> ValidateWebhookSignature(string xSignature, string xRequestId, string dataId);
    }
}
