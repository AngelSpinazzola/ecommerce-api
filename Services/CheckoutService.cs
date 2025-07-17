using EcommerceAPI.DTOs;
using EcommerceAPI.DTOs.MercadoPago;
using EcommerceAPI.Models;
using EcommerceAPI.Repositories;

namespace EcommerceAPI.Services
{
    public class CheckoutService : ICheckoutService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IProductRepository _productRepository;
        private readonly IMercadoPagoService _mercadoPagoService;
        private readonly ILogger<CheckoutService> _logger;
        private readonly IConfiguration _configuration;

        public CheckoutService(
            IOrderRepository orderRepository,
            IPaymentRepository paymentRepository,
            IProductRepository productRepository,
            IMercadoPagoService mercadoPagoService,
            ILogger<CheckoutService> logger,
            IConfiguration configuration)
        {
            Console.WriteLine("🔍 CheckoutService constructor started");
            _orderRepository = orderRepository;
            _paymentRepository = paymentRepository;
            _productRepository = productRepository;
            _mercadoPagoService = mercadoPagoService;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<CheckoutResponseDto> CreateCheckoutAsync(CreateOrderDto createOrderDto, int? userId = null)
        {
            Console.WriteLine("🔍 CheckoutService.CreateCheckoutAsync started");

            try
            {
                // 1. Validar productos y calcular total
                Console.WriteLine("🔍 Step 1: Validating products");
                var orderItems = new List<OrderItem>();
                decimal total = 0;

                foreach (var item in createOrderDto.Items)
                {
                    Console.WriteLine($"🔍 Processing item ProductId: {item.ProductId}");
                    var product = await _productRepository.GetByIdAsync(item.ProductId);
                    if (product == null)
                        throw new ArgumentException($"Producto con ID {item.ProductId} no encontrado");
                    if (product.Stock < item.Quantity)
                        throw new ArgumentException($"Stock insuficiente para {product.Name}");
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

                Console.WriteLine($"🔍 Products validated. Total: {total}");

                // 2. Crear orden
                Console.WriteLine("🔍 Step 2: Creating order");
                var order = new Order
                {
                    CustomerName = createOrderDto.CustomerName,
                    CustomerEmail = createOrderDto.CustomerEmail,
                    CustomerPhone = createOrderDto.CustomerPhone ?? "",
                    CustomerAddress = createOrderDto.CustomerAddress ?? "",
                    Total = total,
                    Status = "pending",
                    UserId = userId,
                    OrderItems = orderItems
                };
                var createdOrder = await _orderRepository.CreateAsync(order);
                Console.WriteLine($"🔍 Order created with ID: {createdOrder.Id}");

                // 3. Crear preferencia de MercadoPago
                Console.WriteLine("🔍 Step 3: Creating MercadoPago preference");
                var preference = await CreateMercadoPagoPreference(createdOrder);
                Console.WriteLine($"🔍 MercadoPago preference created: {preference.Id}");

                // 4. Crear registro de pago
                Console.WriteLine("🔍 Step 4: Creating payment record");
                var payment = new Payment
                {
                    OrderId = createdOrder.Id,
                    PreferenceId = preference.Id,
                    Amount = total,
                    Status = "pending",
                    PayerEmail = createOrderDto.CustomerEmail
                };
                await _paymentRepository.CreateAsync(payment);
                Console.WriteLine("🔍 Payment record created");

                // 5. Reducir stock
                Console.WriteLine("🔍 Step 5: Reducing stock");
                foreach (var item in createOrderDto.Items)
                {
                    var product = await _productRepository.GetByIdAsync(item.ProductId);
                    if (product != null)
                    {
                        product.Stock -= item.Quantity;
                        await _productRepository.UpdateAsync(item.ProductId, product);
                    }
                }

                Console.WriteLine("🔍 Checkout completed successfully");
                _logger.LogInformation("Checkout created successfully. OrderId: {OrderId}, PreferenceId: {PreferenceId}",
                    createdOrder.Id, preference.Id);

                return new CheckoutResponseDto
                {
                    OrderId = createdOrder.Id,
                    MercadoPagoUrl = preference.InitPoint,
                    Total = total,
                    Status = "pending"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔍 ERROR in CheckoutService: {ex.Message}");
                Console.WriteLine($"🔍 StackTrace: {ex.StackTrace}");
                _logger.LogError(ex, "Error creating checkout");
                throw;
            }
        }

        public async Task<bool> ProcessPaymentWebhookAsync(string paymentId, string status, string statusDetail)
        {
            try
            {
                // 1. Obtener info del pago desde MercadoPago
                var paymentInfo = await _mercadoPagoService.GetPaymentInfoAsync(paymentId);

                // 2. Buscar el pago en nuestra DB por external_reference
                var payment = await _paymentRepository.GetByMercadoPagoIdAsync(paymentId) ??
                             await _paymentRepository.GetByOrderIdAsync(int.Parse(paymentInfo.ExternalReference ?? "0"));

                if (payment == null)
                {
                    _logger.LogWarning("Payment not found for MercadoPago ID: {PaymentId}", paymentId);
                    return false;
                }

                // 3. Actualizar estado del pago
                payment.MercadoPagoId = paymentId;
                payment.Status = MapMercadoPagoStatus(status);
                payment.StatusDetail = statusDetail;
                payment.PaymentTypeId = paymentInfo.PaymentTypeId;

                await _paymentRepository.UpdateAsync(payment.Id, payment);

                // 4. Actualizar estado de la orden
                string orderStatus = payment.Status switch
                {
                    "approved" => "completed",
                    "rejected" => "cancelled",
                    "cancelled" => "cancelled",
                    _ => "pending"
                };

                await _orderRepository.UpdateStatusAsync(payment.OrderId, orderStatus);

                _logger.LogInformation("Payment webhook processed. OrderId: {OrderId}, Status: {Status}",
                    payment.OrderId, payment.Status);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment webhook for payment: {PaymentId}", paymentId);
                return false;
            }
        }

        public async Task<OrderDto?> GetOrderWithPaymentAsync(int orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null) return null;

            var payment = await _paymentRepository.GetByOrderIdAsync(orderId);

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

        private async Task<CreatePreferenceResponseDto> CreateMercadoPagoPreference(Order order)
        {
            var items = order.OrderItems.Select(oi => new PreferenceItemDto
            {
                Title = oi.ProductName,
                Description = $"Cantidad: {oi.Quantity}",
                PictureUrl = oi.ProductImageUrl ?? "",
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice
            }).ToList();

            var preference = new CreatePreferenceDto
            {
                Items = items,
                ExternalReference = order.Id.ToString(),
                Payer = new PreferencePayerDto
                {
                    Email = order.CustomerEmail
                },
                NotificationUrl = $"{_configuration["App:BaseUrl"]}/api/checkout/webhook"
            };

            return await _mercadoPagoService.CreatePreferenceAsync(preference);
        }

        private static string MapMercadoPagoStatus(string mercadoPagoStatus)
        {
            return mercadoPagoStatus.ToLower() switch
            {
                "approved" => "approved",
                "pending" => "pending",
                "in_process" => "pending",
                "rejected" => "rejected",
                "cancelled" => "cancelled",
                "refunded" => "refunded",
                _ => "pending"
            };
        }
    }
}
