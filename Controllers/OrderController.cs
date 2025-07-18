using EcommerceAPI.DTOs;
using EcommerceAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EcommerceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        // POST: api/order
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto createOrderDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Obtiene UserId si el usuario está autenticado
                int? userId = null;
                if (User.Identity?.IsAuthenticated == true)
                {
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(userIdClaim, out int parsedUserId))
                    {
                        userId = parsedUserId;
                    }
                }

                var order = await _orderService.CreateOrderAsync(createOrderDto, userId);
                return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // GET: api/order/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            try
            {
                var order = await _orderService.GetOrderByIdAsync(id);
                if (order == null)
                {
                    return NotFound(new { message = "Orden no encontrada" });
                }

                // Verifica permisos de acceso
                bool canAccess = false;

                if (User.Identity?.IsAuthenticated == true)
                {
                    // Usuario autenticado
                    var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    if (userRole == "Admin")
                    {
                        // Los admins pueden ver cualquier orden
                        canAccess = true;
                    }
                    else if (int.TryParse(userIdClaim, out int userId) && order.UserId == userId)
                    {
                        // El usuario puede ver sus propias órdenes
                        canAccess = true;
                    }
                }

                if (!canAccess)
                {
                    return Forbid("No tienes permisos para ver esta orden");
                }

                return Ok(order);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // GET: api/order
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllOrders()
        {
            try
            {
                var orders = await _orderService.GetAllOrdersAsync();
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // GET: api/order/my-orders
        [HttpGet("my-orders")]
        [Authorize]
        public async Task<IActionResult> GetMyOrders()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return BadRequest(new { message = "Usuario no válido" });
                }

                var orders = await _orderService.GetOrdersByUserIdAsync(userId);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // GET: api/order/status/{status}
        [HttpGet("status/{status}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetOrdersByStatus(string status)
        {
            try
            {
                var orders = await _orderService.GetOrdersByStatusAsync(status);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // PUT: api/order/{id}/status
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto updateStatusDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _orderService.UpdateOrderStatusAsync(id, updateStatusDto.Status, updateStatusDto.AdminNotes);
                if (!result)
                {
                    return NotFound(new { message = "Orden no encontrada" });
                }

                return Ok(new { message = "Estado de la orden actualizado correctamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        //// POST: api/order/{id}/payment-receipt
        //[HttpPost("{id}/payment-receipt")]
        //[Authorize]
        //public async Task<IActionResult> UploadPaymentReceipt(int id, [FromForm] IFormFile receiptFile)
        //{
        //    try
        //    {
        //        if (receiptFile == null || receiptFile.Length == 0)
        //        {
        //            return BadRequest(new { message = "No se proporcionó archivo de comprobante" });
        //        }

        //        // Verifica que el archivo sea válido (imagen o PDF)
        //        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
        //        var fileExtension = Path.GetExtension(receiptFile.FileName).ToLowerInvariant();
        //        if (!allowedExtensions.Contains(fileExtension))
        //        {
        //            return BadRequest(new { message = "Formato de archivo no válido. Solo se permiten JPG, PNG o PDF" });
        //        }

        //        // Verifica tamaño (máximo 5MB)
        //        if (receiptFile.Length > 5 * 1024 * 1024)
        //        {
        //            return BadRequest(new { message = "El archivo no puede exceder 5MB" });
        //        }

        //        // Verifica permisos
        //        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        //        if (userRole != "Admin" && !await _orderService.CanUserAccessOrderAsync(id, int.Parse(userIdClaim)))
        //        {
        //            return Forbid("No tienes permisos para subir comprobante a esta orden");
        //        }

        //        var result = await _orderService.UploadPaymentReceiptAsync(id, receiptFile);
        //        if (!result)
        //        {
        //            return NotFound(new { message = "Orden no encontrada o no está en estado válido para subir comprobante" });
        //        }

        //        return Ok(new { message = "Comprobante de pago subido correctamente. Tu orden está ahora en revisión." });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        //    }
        //}

        // GET: api/order/pending-review
        [HttpGet("pending-review")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetOrdersPendingReview()
        {
            try
            {
                var orders = await _orderService.GetOrdersByStatusAsync("payment_submitted");
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // PUT: api/order/{id}/approve-payment
        [HttpPut("{id}/approve-payment")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApprovePayment(int id, [FromBody] AdminActionDto adminActionDto)
        {
            try
            {
                var result = await _orderService.ApprovePaymentAsync(id, adminActionDto.AdminNotes);
                if (!result)
                {
                    return NotFound(new { message = "Orden no encontrada o no está en estado válido para aprobar" });
                }

                return Ok(new { message = "Pago aprobado correctamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // PUT: api/order/{id}/reject-payment
        [HttpPut("{id}/reject-payment")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectPayment(int id, [FromBody] AdminActionDto adminActionDto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(adminActionDto.AdminNotes))
                {
                    return BadRequest(new { message = "Se requiere especificar el motivo del rechazo" });
                }

                var result = await _orderService.RejectPaymentAsync(id, adminActionDto.AdminNotes);
                if (!result)
                {
                    return NotFound(new { message = "Orden no encontrada o no está en estado válido para rechazar" });
                }

                return Ok(new { message = "Pago rechazado" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // PUT: api/order/{id}/mark-shipped
        [HttpPut("{id}/mark-shipped")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> MarkAsShipped(int id, [FromBody] ShippingInfoDto shippingInfoDto)
        {
            try
            {
                var result = await _orderService.MarkAsShippedAsync(id, shippingInfoDto.TrackingNumber, shippingInfoDto.ShippingProvider, shippingInfoDto.AdminNotes);
                if (!result)
                {
                    return NotFound(new { message = "Orden no encontrada o no está en estado válido para marcar como enviado" });
                }

                return Ok(new { message = "Orden marcada como enviada correctamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // PUT: api/order/{id}/mark-delivered
        [HttpPut("{id}/mark-delivered")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> MarkAsDelivered(int id, [FromBody] AdminActionDto adminActionDto)
        {
            try
            {
                var result = await _orderService.MarkAsDeliveredAsync(id, adminActionDto.AdminNotes);
                if (!result)
                {
                    return NotFound(new { message = "Orden no encontrada o no está en estado válido para marcar como entregado" });
                }

                return Ok(new { message = "Orden marcada como entregada correctamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // GET: api/order/{id}/payment-receipt
        [HttpGet("{id}/payment-receipt")]
        [Authorize]
        public async Task<IActionResult> GetPaymentReceipt(int id)
        {
            try
            {
                // Verifica permisos
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (userRole != "Admin" && !await _orderService.CanUserAccessOrderAsync(id, int.Parse(userIdClaim)))
                {
                    return Forbid("No tienes permisos para ver el comprobante de esta orden");
                }

                var receiptUrl = await _orderService.GetPaymentReceiptUrlAsync(id);
                if (string.IsNullOrEmpty(receiptUrl))
                {
                    return NotFound(new { message = "No se encontró comprobante para esta orden" });
                }

                return Ok(new { receiptUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }
    }
}
