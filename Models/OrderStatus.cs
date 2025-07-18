namespace EcommerceAPI.Models
{
    public static class OrderStatus
    {
        public const string PendingPayment = "pending_payment";        // Esperando que adjunte comprobante
        public const string PaymentSubmitted = "payment_submitted";    // Comprobante adjuntado, en revisión
        public const string PaymentApproved = "payment_approved";      // Pago aprobado, preparando envío
        public const string PaymentRejected = "payment_rejected";      // Comprobante rechazado
        public const string Shipped = "shipped";                      // Enviado
        public const string Delivered = "delivered";                  // Entregado
        public const string Cancelled = "cancelled";                  // Cancelado

        // Método helper para validar estados
        public static bool IsValidStatus(string status)
        {
            return status switch
            {
                PendingPayment or PaymentSubmitted or PaymentApproved or
                PaymentRejected or Shipped or Delivered or Cancelled => true,
                _ => false
            };
        }

        public static string GetStatusDescription(string status)
        {
            return status switch
            {
                PendingPayment => "Esperando comprobante de pago",
                PaymentSubmitted => "Comprobante en revisión",
                PaymentApproved => "Pago aprobado - Preparando envío",
                PaymentRejected => "Comprobante rechazado",
                Shipped => "Enviado",
                Delivered => "Entregado",
                Cancelled => "Cancelado",
                _ => "Estado desconocido"
            };
        }
    }
}
