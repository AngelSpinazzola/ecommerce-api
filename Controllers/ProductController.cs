using EcommerceAPI.DTOs;
using EcommerceAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        // GET: api/product
        [HttpGet]
        public async Task<IActionResult> GetAllProducts()
        {
            try
            {
                var products = await _productService.GetAllProductsAsync();
                return Ok(products);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // GET: api/product/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    return NotFound(new { message = "Producto no encontrado" });
                }

                return Ok(product);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // POST: api/product
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateProduct([FromForm] CreateProductDto createProductDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var product = await _productService.CreateProductAsync(createProductDto);
                return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, product);
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

        // PUT: api/product/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProduct(int id, [FromForm] UpdateProductDto updateProductDto)
        {
            try
            {
                Console.WriteLine($"🔍 Update Product ID: {id}");
                Console.WriteLine($"🔍 DTO Name: {updateProductDto.Name}");
                Console.WriteLine($"🔍 DTO Price: {updateProductDto.Price}");
                Console.WriteLine($"🔍 DTO Stock: {updateProductDto.Stock}");
                Console.WriteLine($"🔍 DTO Category: {updateProductDto.Category}");
                Console.WriteLine($"🔍 DTO IsActive: {updateProductDto.IsActive}");
                Console.WriteLine($"🔍 DTO ImageFiles count: {updateProductDto.ImageFiles?.Length ?? 0}");
                Console.WriteLine($"🔍 ModelState.IsValid: {ModelState.IsValid}");
                if (!ModelState.IsValid)
                {
                    Console.WriteLine("🚨 ModelState errors:");
                    foreach (var error in ModelState)
                    {
                        Console.WriteLine($"   {error.Key}: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                    }
                    return BadRequest(ModelState);
                }

                var product = await _productService.UpdateProductAsync(id, updateProductDto);
                if (product == null)
                {
                    return NotFound(new { message = "Producto no encontrado" });
                }

                return Ok(product);
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

        // DELETE: api/product/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var result = await _productService.DeleteProductAsync(id);
                if (!result)
                {
                    return NotFound(new { message = "Producto no encontrado" });
                }

                return Ok(new { message = "Producto eliminado correctamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // GET: api/product/category/{category}
        [HttpGet("category/{category}")]
        public async Task<IActionResult> GetProductsByCategory(string category)
        {
            try
            {
                var products = await _productService.GetProductsByCategoryAsync(category);
                return Ok(products);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // GET: api/product/search?term={searchTerm}
        [HttpGet("search")]
        public async Task<IActionResult> SearchProducts([FromQuery] string term)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term))
                {
                    return BadRequest(new { message = "El término de búsqueda es requerido" });
                }

                var products = await _productService.SearchProductsAsync(term);
                return Ok(products);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // GET: api/product/categories
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _productService.GetCategoriesAsync();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }
    }
}
