using EcommerceAPI.DTOs;
using EcommerceAPI.Models;
using EcommerceAPI.Repositories;

namespace EcommerceAPI.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IProductRepository _productRepository;
        private readonly IFileService _fileService; // Agregar este servicio

        public OrderService(
            IOrderRepository orderRepository,
            IProductRepository productRepository,
            IFileService fileService) // Agregar aquí
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
            _fileService = fileService;
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

            // 2. Crea la orden con el nuevo status de pending_payment
            var order = new Order
            {
                CustomerName = createOrderDto.CustomerName,
                CustomerEmail = createOrderDto.CustomerEmail,
                CustomerPhone = createOrderDto.CustomerPhone,
                CustomerAddress = createOrderDto.CustomerAddress,
                Total = total,
                Status = OrderStatus.PendingPayment, // Cambiar aquí
                PaymentMethod = "bank_transfer",
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

            // 4. NO marcar como completada - esperar pago
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

        public async Task<IEnumerable<OrderSummaryDto>> GetOrdersByStatusAsync(string status)
        {
            var orders = await _orderRepository.GetByStatusAsync(status);
            return orders.Select(MapToOrderSummaryDto);
        }

        // Método existente actualizado
        public async Task<bool> UpdateOrderStatusAsync(int id, string status, string? adminNotes = null)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null) return false;

            order.Status = status;
            order.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(adminNotes))
            {
                order.AdminNotes = adminNotes;
            }

            return await _orderRepository.UpdateAsync(order.Id, order) != null;
        }

        public async Task<bool> UploadPaymentReceiptAsync(int orderId, IFormFile receiptFile)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);

            Console.WriteLine($"🔍 Order found: {order != null}");

            if (order == null || (order.Status != OrderStatus.PendingPayment && order.Status != OrderStatus.PaymentRejected))
            {
                Console.WriteLine($"❌ Invalid status or order not found. Status: {order?.Status}");
                return false;
            }

            Console.WriteLine($"🔍 Order status: {order.Status}");
            Console.WriteLine($"🔍 Order ID: {order.Id}");
            try
            {
                // Guarda el archivo del comprobante
                var receiptUrl = await _fileService.SaveImageAsync(receiptFile, "receipts");

                // Actualiza la orden
                order.PaymentReceiptUrl = receiptUrl;
                order.PaymentReceiptUploadedAt = DateTime.UtcNow;
                order.Status = OrderStatus.PaymentSubmitted;
                order.UpdatedAt = DateTime.UtcNow;

                var updatedOrder = await _orderRepository.UpdateAsync(order.Id, order);
                return updatedOrder != null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al subir comprobante: {ex.Message}");
                return false;
            }
        }
                                                            
        public async Task<bool> ApprovePaymentAsync(int orderId, string? adminNotes = null)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null || order.Status != OrderStatus.PaymentSubmitted)
                return false;

            order.Status = OrderStatus.PaymentApproved;
            order.PaymentApprovedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(adminNotes))
            {
                order.AdminNotes = adminNotes;
            }

            var updatedOrder = await _orderRepository.UpdateAsync(order.Id, order);
            return updatedOrder != null;
        }

        public async Task<bool> RejectPaymentAsync(int orderId, string adminNotes)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null || order.Status != OrderStatus.PaymentSubmitted)
                return false;

            order.Status = OrderStatus.PaymentRejected;
            order.AdminNotes = adminNotes;
            order.UpdatedAt = DateTime.UtcNow;

            var updatedOrder = await _orderRepository.UpdateAsync(order.Id, order);
            return updatedOrder != null;
        }

        public async Task<bool> MarkAsShippedAsync(int orderId, string trackingNumber, string shippingProvider, string? adminNotes = null)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null || order.Status != OrderStatus.PaymentApproved)
                return false;

            order.Status = OrderStatus.Shipped;
            order.TrackingNumber = trackingNumber;
            order.ShippingProvider = shippingProvider;
            order.ShippedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(adminNotes))
            {
                order.AdminNotes = adminNotes;
            }

            var updatedOrder = await _orderRepository.UpdateAsync(order.Id, order);
            return updatedOrder != null;
        }

        public async Task<bool> MarkAsDeliveredAsync(int orderId, string? adminNotes = null)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null || order.Status != OrderStatus.Shipped)
                return false;

            order.Status = OrderStatus.Delivered;
            order.DeliveredAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(adminNotes))
            {
                order.AdminNotes = adminNotes;
            }

            var updatedOrder = await _orderRepository.UpdateAsync(order.Id, order);
            return updatedOrder != null;
        }

        public async Task<bool> CanUserAccessOrderAsync(int orderId, int userId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            return order?.UserId == userId;
        }

        public async Task<string?> GetPaymentReceiptUrlAsync(int orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            return order?.PaymentReceiptUrl;
        }

        // ===== MAPPERS ACTUALIZADOS =====
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
                StatusDescription = OrderStatus.GetStatusDescription(order.Status),

                // Información de pago
                PaymentMethod = order.PaymentMethod,
                PaymentReceiptUrl = order.PaymentReceiptUrl,
                PaymentReceiptUploadedAt = order.PaymentReceiptUploadedAt,
                PaymentApprovedAt = order.PaymentApprovedAt,
                ShippedAt = order.ShippedAt,
                DeliveredAt = order.DeliveredAt,

                // Información de administración
                AdminNotes = order.AdminNotes,
                TrackingNumber = order.TrackingNumber,
                ShippingProvider = order.ShippingProvider,

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
                StatusDescription = OrderStatus.GetStatusDescription(order.Status),

                // Información de pago resumida
                HasPaymentReceipt = !string.IsNullOrEmpty(order.PaymentReceiptUrl),
                PaymentReceiptUploadedAt = order.PaymentReceiptUploadedAt,
                TrackingNumber = order.TrackingNumber,

                CreatedAt = order.CreatedAt,
                ItemsCount = order.OrderItems?.Count ?? 0,
                UserId = order.UserId
            };
        }
    }
}
