using EcommerceAPI.DTOs;
using EcommerceAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.Controllers
{
    [ApiController]
    [Route("api/product/{productId}/images")]
    public class ProductImageController : ControllerBase
    {
        private readonly IProductImageService _productImageService;

        public ProductImageController(IProductImageService productImageService)
        {
            _productImageService = productImageService;
        }

        // GET: api/product/{productId}/images
        [HttpGet]
        public async Task<IActionResult> GetProductImages(int productId)
        {
            try
            {
                var images = await _productImageService.GetProductImagesAsync(productId);
                return Ok(images);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // GET: api/product/{productId}/images/{imageId}
        [HttpGet("{imageId}")]
        public async Task<IActionResult> GetProductImage(int productId, int imageId)
        {
            try
            {
                var image = await _productImageService.GetProductImageAsync(productId, imageId);
                if (image == null)
                {
                    return NotFound(new { message = "Imagen no encontrada" });
                }

                return Ok(image);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // POST: api/product/{productId}/images
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateProductImages(int productId, [FromForm] CreateProductImageDto createDto)
        {
            try
            {
                Console.WriteLine($"🔍 ProductId: {productId}");
                Console.WriteLine($"🔍 CreateDto.ProductId: {createDto.ProductId}");
                Console.WriteLine($"🔍 ImageFiles count: {createDto.ImageFiles?.Length ?? 0}");
                Console.WriteLine($"🔍 MainImageIndex: {createDto.MainImageIndex}");
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

                // Asegura que el productId coincida
                createDto.ProductId = productId;

                var images = await _productImageService.CreateProductImagesAsync(createDto);
                return Ok(new { message = "Imágenes agregadas correctamente", images });
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

        // DELETE: api/product/{productId}/images/{imageId}
        [HttpDelete("{imageId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProductImage(int productId, int imageId)
        {
            try
            {
                var result = await _productImageService.DeleteProductImageAsync(productId, imageId);
                if (!result)
                {
                    return NotFound(new { message = "Imagen no encontrada" });
                }

                return Ok(new { message = "Imagen eliminada correctamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // PUT: api/product/{productId}/images/{imageId}/main
        [HttpPut("{imageId}/main")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SetMainImage(int productId, int imageId)
        {
            try
            {
                var result = await _productImageService.SetMainImageAsync(productId, imageId);
                if (!result)
                {
                    return BadRequest(new { message = "No se pudo establecer como imagen principal" });
                }

                return Ok(new { message = "Imagen principal actualizada" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // PUT: api/product/{productId}/images/order
        [HttpPut("order")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateImagesOrder(int productId, [FromBody] UpdateProductImagesOrderDto updateOrderDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _productImageService.UpdateImagesOrderAsync(productId, updateOrderDto);
                if (!result)
                {
                    return BadRequest(new { message = "No se pudo actualizar el orden de las imágenes" });
                }

                return Ok(new { message = "Orden de imágenes actualizado" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // POST: api/product/{productId}/images/upload-multiple
        [HttpPost("upload-multiple")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadMultipleImages(
            int productId,
            [FromForm] IFormFile[] files,
            [FromForm] int? mainImageIndex = null)
        {
            try
            {
                if (files == null || !files.Any())
                {
                    return BadRequest(new { message = "No se proporcionaron archivos" });
                }

                // Crear DTO usando tu estructura existente
                var createImageDto = new CreateProductImageDto
                {
                    ProductId = productId,
                    ImageFiles = files,
                    MainImageIndex = mainImageIndex ?? 0 // Primer imagen por defecto
                };

                // Primero eliminar imágenes existentes del producto
                var existingImages = await _productImageService.GetProductImagesAsync(productId);
                foreach (var existingImage in existingImages)
                {
                    await _productImageService.DeleteProductImageAsync(productId, existingImage.Id);
                }

                // Crear nuevas imágenes usando tu servicio existente
                var uploadedImages = await _productImageService.CreateProductImagesAsync(createImageDto);

                return Ok(uploadedImages);
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
    }
}
