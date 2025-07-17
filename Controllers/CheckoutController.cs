using EcommerceAPI.DTOs;
using EcommerceAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EcommerceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CheckoutController : ControllerBase
    {
        private readonly ICheckoutService _checkoutService;
        private readonly ILogger<CheckoutController> _logger;

        public CheckoutController(ICheckoutService checkoutService, ILogger<CheckoutController> logger)
        {
            _checkoutService = checkoutService;
            _logger = logger;
        }

        // POST: api/checkout
        [HttpPost]
        public async Task<IActionResult> CreateCheckout([FromBody] CreateOrderDto createOrderDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Obtener UserId si el usuario está autenticado
                int? userId = null;
                if (User.Identity?.IsAuthenticated == true)
                {
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(userIdClaim, out int parsedUserId))
                    {
                        userId = parsedUserId;
                    }
                }

                var checkout = await _checkoutService.CreateCheckoutAsync(createOrderDto, userId);

                _logger.LogInformation("Checkout created successfully for order: {OrderId}", checkout.OrderId);

                return Ok(checkout);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Checkout validation error: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating checkout");
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // GET: api/checkout/order/{orderId}
        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetOrderWithPayment(int orderId)
        {
            try
            {
                var order = await _checkoutService.GetOrderWithPaymentAsync(orderId);
                if (order == null)
                {
                    return NotFound(new { message = "Orden no encontrada" });
                }

                // Verificar permisos (similar a tu OrderController)
                bool canAccess = false;

                if (User.Identity?.IsAuthenticated == true)
                {
                    var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    if (userRole == "Admin")
                    {
                        canAccess = true;
                    }
                    else if (int.TryParse(userIdClaim, out int userId) && order.UserId == userId)
                    {
                        canAccess = true;
                    }
                }
                else
                {
                    // Permitir acceso público para guest checkout (opcional)
                    canAccess = order.UserId == null;
                }

                if (!canAccess)
                {
                    return Forbid("No tienes permisos para ver esta orden");
                }

                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order: {OrderId}", orderId);
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // POST: api/checkout/webhook
        [HttpPost("webhook")]
        public async Task<IActionResult> ProcessWebhook()
        {
            try
            {
                // Leer el body del webhook
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();

                _logger.LogInformation("Webhook received: {Body}", body);

                // Parsear los datos del webhook (MercadoPago envía JSON)
                var webhookData = System.Text.Json.JsonSerializer.Deserialize<WebhookNotificationDto>(body);

                if (webhookData?.Data?.Id == null)
                {
                    _logger.LogWarning("Invalid webhook data received");
                    return BadRequest("Invalid webhook data");
                }

                // Procesar el webhook
                var success = await _checkoutService.ProcessPaymentWebhookAsync(
                    webhookData.Data.Id,
                    webhookData.Action ?? "unknown",
                    ""
                );

                if (success)
                {
                    _logger.LogInformation("Webhook processed successfully for payment: {PaymentId}", webhookData.Data.Id);
                    return Ok(new { message = "Webhook procesado correctamente" });
                }
                else
                {
                    _logger.LogWarning("Failed to process webhook for payment: {PaymentId}", webhookData.Data.Id);
                    return BadRequest("Error procesando webhook");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // GET: api/checkout/success?payment_id={id}&status={status}&external_reference={orderId}
        [HttpGet("success")]
        public async Task<IActionResult> PaymentSuccess(
            [FromQuery] string? payment_id,
            [FromQuery] string? status,
            [FromQuery] string? external_reference)
        {
            try
            {
                _logger.LogInformation("Payment success callback - PaymentId: {PaymentId}, Status: {Status}, OrderId: {OrderId}",
                    payment_id, status, external_reference);

                if (string.IsNullOrEmpty(external_reference) || !int.TryParse(external_reference, out int orderId))
                {
                    return BadRequest("Invalid order reference");
                }

                var order = await _checkoutService.GetOrderWithPaymentAsync(orderId);
                if (order == null)
                {
                    return NotFound("Orden no encontrada");
                }

                // Procesar el pago si tenemos el payment_id
                if (!string.IsNullOrEmpty(payment_id) && !string.IsNullOrEmpty(status))
                {
                    await _checkoutService.ProcessPaymentWebhookAsync(payment_id, status, "");
                }

                return Ok(new
                {
                    message = "Pago procesado exitosamente",
                    orderId = orderId,
                    status = status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in payment success callback");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // GET: api/checkout/failure?external_reference={orderId}
        [HttpGet("failure")]
        public async Task<IActionResult> PaymentFailure([FromQuery] string? external_reference)
        {
            try
            {
                _logger.LogInformation("Payment failure callback - OrderId: {OrderId}", external_reference);

                if (string.IsNullOrEmpty(external_reference) || !int.TryParse(external_reference, out int orderId))
                {
                    return BadRequest("Invalid order reference");
                }

                // Actualizar estado de la orden a cancelada
                // (esto se podría hacer en el CheckoutService)

                return Ok(new
                {
                    message = "Pago cancelado",
                    orderId = orderId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in payment failure callback");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new { message = "CheckoutController is working", timestamp = DateTime.UtcNow });
        }
    }
}
