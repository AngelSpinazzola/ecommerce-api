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

                var result = await _orderService.UpdateOrderStatusAsync(id, updateStatusDto.Status);
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
    }
}
