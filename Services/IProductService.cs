using EcommerceAPI.DTOs;

namespace EcommerceAPI.Services
{
    public interface IProductService
    {
        Task<IEnumerable<ProductListDto>> GetAllProductsAsync();
        Task<ProductDto?> GetProductByIdAsync(int id);
        Task<ProductDto> CreateProductAsync(CreateProductDto createProductDto);
        Task<ProductDto?> UpdateProductAsync(int id, UpdateProductDto updateProductDto);
        Task<bool> DeleteProductAsync(int id);
        Task<IEnumerable<ProductListDto>> GetProductsByCategoryAsync(string category);
        Task<IEnumerable<ProductListDto>> SearchProductsAsync(string searchTerm);
        Task<IEnumerable<string>> GetCategoriesAsync();
    }
}
