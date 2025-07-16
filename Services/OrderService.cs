using EcommerceAPI.DTOs;
using EcommerceAPI.Models;
using EcommerceAPI.Repositories;

namespace EcommerceAPI.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IProductRepository _productRepository;

        public OrderService(IOrderRepository orderRepository, IProductRepository productRepository)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
        }

        public async Task<OrderDto> CreateOrderAsync(CreateOrderDto createOrderDto, int? userId = null)
        {
            // 1. Valida que todos los productos existan y tengan stock suficiente
            var orderItems = new List<OrderItem>();
            decimal total = 0;

            foreach (var item in createOrderDto.Items)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId);
                if (product == null)
                    throw new ArgumentException($"Producto con ID {item.ProductId} no encontrado");

                if (product.Stock < item.Quantity)
                    throw new ArgumentException($"Stock insuficiente para {product.Name}. Disponible: {product.Stock}, Solicitado: {item.Quantity}");

                if (!product.IsActive)
                    throw new ArgumentException($"El producto {product.Name} no está disponible");

                var subtotal = product.Price * item.Quantity;
                total += subtotal;

                orderItems.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price,
                    Subtotal = subtotal,
                    ProductName = product.Name,
                    ProductImageUrl = product.MainImageUrl
                });
            }

            // 2. Crea la orden
            var order = new Order
            {
                CustomerName = createOrderDto.CustomerName,
                CustomerEmail = createOrderDto.CustomerEmail,
                CustomerPhone = createOrderDto.CustomerPhone,
                CustomerAddress = createOrderDto.CustomerAddress,
                Total = total,
                Status = "pending",
                UserId = userId,
                OrderItems = orderItems
            };

            var createdOrder = await _orderRepository.CreateAsync(order);

            // 3. Descuenta stock de los productos
            foreach (var item in createOrderDto.Items)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId);
                if (product != null)
                {
                    product.Stock -= item.Quantity;
                    product.UpdatedAt = DateTime.UtcNow;
                    await _productRepository.UpdateAsync(product.Id, product);
                }
            }

            // 4. Actualiza estado de la orden a completada (por ahora, sin pagos)
            await _orderRepository.UpdateStatusAsync(createdOrder.Id, "completed");

            // 5. Obtiene la orden actualizada y convierte a DTO
            var finalOrder = await _orderRepository.GetByIdAsync(createdOrder.Id);
            return MapToOrderDto(finalOrder!);
        }

        public async Task<OrderDto?> GetOrderByIdAsync(int id)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            return order == null ? null : MapToOrderDto(order);
        }

        public async Task<IEnumerable<OrderSummaryDto>> GetAllOrdersAsync()
        {
            var orders = await _orderRepository.GetAllAsync();
            return orders.Select(MapToOrderSummaryDto);
        }

        public async Task<IEnumerable<OrderSummaryDto>> GetOrdersByUserIdAsync(int userId)
        {
            var orders = await _orderRepository.GetByUserIdAsync(userId);
            return orders.Select(MapToOrderSummaryDto);
        }

        public async Task<bool> UpdateOrderStatusAsync(int id, string status)
        {
            return await _orderRepository.UpdateStatusAsync(id, status);
        }

        public async Task<IEnumerable<OrderSummaryDto>> GetOrdersByStatusAsync(string status)
        {
            var orders = await _orderRepository.GetByStatusAsync(status);
            return orders.Select(MapToOrderSummaryDto);
        }

        // Mappers
        private OrderDto MapToOrderDto(Order order)
        {
            return new OrderDto
            {
                Id = order.Id,
                CustomerName = order.CustomerName,
                CustomerEmail = order.CustomerEmail,
                CustomerPhone = order.CustomerPhone,
                CustomerAddress = order.CustomerAddress,
                Total = order.Total,
                Status = order.Status,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                UserId = order.UserId,
                OrderItems = order.OrderItems.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    ProductId = oi.ProductId,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    Subtotal = oi.Subtotal,
                    ProductName = oi.ProductName,
                    ProductImageUrl = oi.ProductImageUrl
                }).ToList()
            };
        }

        private OrderSummaryDto MapToOrderSummaryDto(Order order)
        {
            return new OrderSummaryDto
            {
                Id = order.Id,
                CustomerName = order.CustomerName,
                CustomerEmail = order.CustomerEmail,
                Total = order.Total,
                Status = order.Status,
                CreatedAt = order.CreatedAt,
                ItemsCount = order.OrderItems?.Count ?? 0
            };
        }
    }
}
