namespace EcommerceAPI.DTOs.MercadoPago
{
    public class PreferenceItemDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PictureUrl { get; set; } = string.Empty;
        public string CategoryId { get; set; } = "others";
        public int Quantity { get; set; }
        public string CurrencyId { get; set; } = "ARS";
        public decimal UnitPrice { get; set; }
    }
}
