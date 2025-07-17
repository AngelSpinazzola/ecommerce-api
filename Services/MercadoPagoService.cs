using EcommerceAPI.DTOs.MercadoPago;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EcommerceAPI.Services
{
    public class MercadoPagoService : IMercadoPagoService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<MercadoPagoService> _logger;
        private readonly string _accessToken;
        private readonly string _webhookSecret;
        private const string BaseUrl = "https://api.mercadopago.com";

        public MercadoPagoService(HttpClient httpClient, IConfiguration configuration, ILogger<MercadoPagoService> logger)
        {
            Console.WriteLine("🔍 MercadoPagoService constructor started");

            _httpClient = httpClient;
            _logger = logger;

            // Test TODAS las formas posibles
            Console.WriteLine("🔍 Testing all configuration formats:");
            Console.WriteLine($"🔍 MercadoPago:AccessToken = '{configuration["MercadoPago:AccessToken"]}'");
            Console.WriteLine($"🔍 MercadoPago__AccessToken = '{configuration["MercadoPago__AccessToken"]}'");
            Console.WriteLine($"🔍 MERCADOPAGO_ACCESSTOKEN = '{configuration["MERCADOPAGO_ACCESSTOKEN"]}'");

            // Test environment variables directamente
            Console.WriteLine($"🔍 Environment MercadoPago__AccessToken = '{Environment.GetEnvironmentVariable("MercadoPago__AccessToken")}'");

            // Mostrar TODAS las variables que empiecen con "Mercado"
            Console.WriteLine("🔍 All configuration keys starting with 'Mercado':");
            foreach (var config in configuration.AsEnumerable())
            {
                if (config.Key.StartsWith("Mercado", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"   {config.Key} = '{config.Value}'");
                }
            }

            _accessToken = configuration["MercadoPago:AccessToken"] ?? throw new ArgumentException("MercadoPago AccessToken is required");
            _webhookSecret = configuration["MercadoPago:WebhookSecret"] ?? string.Empty;

            // Configurar HttpClient
            _httpClient.BaseAddress = new Uri(BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
            _httpClient.DefaultRequestHeaders.Add("X-Idempotency-Key", Guid.NewGuid().ToString());
        }


        public async Task<CreatePreferenceResponseDto> CreatePreferenceAsync(CreatePreferenceDto preferenceDto)
        {
            try
            {
                var jsonContent = JsonSerializer.Serialize(preferenceDto, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                Console.WriteLine($"🔍 Sending to MercadoPago: {jsonContent}");
                Console.WriteLine($"🔍 Using AccessToken: {_accessToken?.Substring(0, 20)}...");

                _logger.LogInformation("Creating MercadoPago preference: {Content}", jsonContent);

                var response = await _httpClient.PostAsync("/checkout/preferences", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"🔍 MercadoPago Response Status: {response.StatusCode}");
                Console.WriteLine($"🔍 MercadoPago Response Body: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("MercadoPago API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    throw new Exception($"Error creating preference: {response.StatusCode}");
                }

                var preference = JsonSerializer.Deserialize<CreatePreferenceResponseDto>(responseContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });

                _logger.LogInformation("MercadoPago preference created successfully: {PreferenceId}", preference?.Id);

                return preference ?? throw new Exception("Invalid response from MercadoPago");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating MercadoPago preference");
                throw;
            }
        }

        public async Task<PaymentInfoDto> GetPaymentInfoAsync(string paymentId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v1/payments/{paymentId}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Error getting payment info: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    throw new Exception($"Error getting payment info: {response.StatusCode}");
                }

                var payment = JsonSerializer.Deserialize<PaymentInfoDto>(responseContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });

                return payment ?? throw new Exception("Invalid payment response");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment info for ID: {PaymentId}", paymentId);
                throw;
            }
        }

        public async Task<bool> ValidateWebhookSignature(string xSignature, string xRequestId, string dataId)
        {
            try
            {
                if (string.IsNullOrEmpty(_webhookSecret))
                {
                    _logger.LogWarning("Webhook secret not configured, skipping signature validation");
                    return true; // En desarrollo, permitir sin validación
                }

                // Implementar validación de firma de webhook según documentación de MercadoPago
                // Por ahora, devolver true para desarrollo
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating webhook signature");
                return false;
            }
        }
    }
}
