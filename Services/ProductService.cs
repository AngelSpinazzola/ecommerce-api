using EcommerceAPI.DTOs;
using EcommerceAPI.Models;
using EcommerceAPI.Repositories;

namespace EcommerceAPI.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly IProductImageRepository _productImageRepository;
        private readonly IFileService _fileService;

        public ProductService(
            IProductRepository productRepository,
            IProductImageRepository productImageRepository,
            IFileService fileService)
        {
            _productRepository = productRepository;
            _productImageRepository = productImageRepository;
            _fileService = fileService;
        }

        public async Task<IEnumerable<ProductListDto>> GetAllProductsAsync()
        {
            var products = await _productRepository.GetAllAsync();
            return products.Select(p => new ProductListDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                Stock = p.Stock,
                Category = p.Category,
                MainImageUrl = p.MainImageUrl,
                IsActive = p.IsActive
            });
        }

        public async Task<ProductDto?> GetProductByIdAsync(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
                return null;

            // Obtiene imágenes del producto
            var images = await _productImageRepository.GetByProductIdAsync(id);

            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Stock = product.Stock,
                Category = product.Category,
                MainImageUrl = product.MainImageUrl,
                IsActive = product.IsActive,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt,
                Images = images.Select(img => new ProductImageDto
                {
                    Id = img.Id,
                    ProductId = img.ProductId,
                    ImageUrl = img.ImageUrl,
                    DisplayOrder = img.DisplayOrder,
                    IsMain = img.IsMain,
                    CreatedAt = img.CreatedAt
                }).ToList()
            };
        }

        public async Task<ProductDto> CreateProductAsync(CreateProductDto createProductDto)
        {
            var product = new Product
            {
                Name = createProductDto.Name,
                Description = createProductDto.Description,
                Price = createProductDto.Price,
                Stock = createProductDto.Stock,
                Category = createProductDto.Category,
                IsActive = true
            };

            // Crea producto primero
            var createdProduct = await _productRepository.CreateAsync(product);

            // Maneja imágenes
            string mainImageUrl = null;
            var productImages = new List<ProductImage>();

            // Procesa archivos de imagen
            if (createProductDto.ImageFiles != null && createProductDto.ImageFiles.Length > 0)
            {
                for (int i = 0; i < createProductDto.ImageFiles.Length; i++)
                {
                    var imageFile = createProductDto.ImageFiles[i];
                    var imageUrl = await _fileService.SaveImageAsync(imageFile);

                    var productImage = new ProductImage
                    {
                        ProductId = createdProduct.Id,
                        ImageUrl = imageUrl,
                        DisplayOrder = i,
                        IsMain = i == 0 // Primera imagen es principal
                    };

                    var createdImage = await _productImageRepository.CreateAsync(productImage);
                    productImages.Add(createdImage);

                    if (i == 0) mainImageUrl = imageUrl;
                }
            }

            // Procesa URLs de imagen
            if (createProductDto.ImageUrls != null && createProductDto.ImageUrls.Length > 0)
            {
                int startOrder = createProductDto.ImageFiles?.Length ?? 0;
                for (int i = 0; i < createProductDto.ImageUrls.Length; i++)
                {
                    var imageUrl = createProductDto.ImageUrls[i];
                    if (!string.IsNullOrWhiteSpace(imageUrl))
                    {
                        var productImage = new ProductImage
                        {
                            ProductId = createdProduct.Id,
                            ImageUrl = imageUrl,
                            DisplayOrder = startOrder + i,
                            IsMain = startOrder == 0 && i == 0 // Primera imagen si no hay archivos
                        };

                        var createdImage = await _productImageRepository.CreateAsync(productImage);
                        productImages.Add(createdImage);

                        if (startOrder == 0 && i == 0) mainImageUrl = imageUrl;
                    }
                }
            }

            // Si no hay imágenes, usa placeholder
            if (string.IsNullOrEmpty(mainImageUrl))
            {
                mainImageUrl = "https://picsum.photos/400/300?random=" + new Random().Next(1, 1000);
            }

            // Actualiza producto con imagen principal
            createdProduct.MainImageUrl = mainImageUrl;
            await _productRepository.UpdateAsync(createdProduct.Id, createdProduct);

            return new ProductDto
            {
                Id = createdProduct.Id,
                Name = createdProduct.Name,
                Description = createdProduct.Description,
                Price = createdProduct.Price,
                Stock = createdProduct.Stock,
                Category = createdProduct.Category,
                MainImageUrl = createdProduct.MainImageUrl,
                IsActive = createdProduct.IsActive,
                CreatedAt = createdProduct.CreatedAt,
                UpdatedAt = createdProduct.UpdatedAt,
                Images = productImages.Select(img => new ProductImageDto
                {
                    Id = img.Id,
                    ProductId = img.ProductId,
                    ImageUrl = img.ImageUrl,
                    DisplayOrder = img.DisplayOrder,
                    IsMain = img.IsMain,
                    CreatedAt = img.CreatedAt
                }).ToList()
            };
        }

        public async Task<ProductDto?> UpdateProductAsync(int id, UpdateProductDto updateProductDto)
        {
            var existingProduct = await _productRepository.GetByIdAsync(id);
            if (existingProduct == null)
                return null;

            var product = new Product
            {
                Id = id,  // ← AGREGAR ESTO
                Name = updateProductDto.Name,
                Description = updateProductDto.Description,
                Price = updateProductDto.Price,
                Stock = updateProductDto.Stock,
                Category = updateProductDto.Category,
                IsActive = updateProductDto.IsActive,
                MainImageUrl = existingProduct.MainImageUrl,
                CreatedAt = existingProduct.CreatedAt,  // ← AGREGAR ESTO
                UpdatedAt = DateTime.UtcNow
            };

            // Procesa nuevas imágenes si se proporcionan
            if (updateProductDto.ImageFiles != null && updateProductDto.ImageFiles.Length > 0)
            {
                var existingImages = await _productImageRepository.GetByProductIdAsync(id);
                int startOrder = existingImages.Any() ? existingImages.Max(img => img.DisplayOrder) + 1 : 0;

                for (int i = 0; i < updateProductDto.ImageFiles.Length; i++)
                {
                    var imageFile = updateProductDto.ImageFiles[i];
                    var imageUrl = await _fileService.SaveImageAsync(imageFile);

                    var productImage = new ProductImage
                    {
                        ProductId = id,
                        ImageUrl = imageUrl,
                        DisplayOrder = startOrder + i,
                        IsMain = !existingImages.Any() && i == 0 // Principal si es la primera imagen del producto
                    };

                    await _productImageRepository.CreateAsync(productImage);

                    // Si es la primera imagen y no hay imagen principal, establecerla
                    if (!existingImages.Any() && i == 0)
                    {
                        product.MainImageUrl = imageUrl;
                    }
                }
            }

            var updatedProduct = await _productRepository.UpdateAsync(id, product);
            if (updatedProduct == null)
                return null;

            // Obtiene todas las imágenes actualizadas
            var images = await _productImageRepository.GetByProductIdAsync(id);

            // 🔍 AGREGAR ESTE LOG
            Console.WriteLine($"🔍 Product {id} - Found {images.Count()} images in DB");
            foreach (var img in images)
            {
                Console.WriteLine($"   Image ID: {img.Id}, Order: {img.DisplayOrder}, Main: {img.IsMain}, URL: {img.ImageUrl}");
            }

            return new ProductDto
            {
                Id = updatedProduct.Id,
                Name = updatedProduct.Name,
                Description = updatedProduct.Description,
                Price = updatedProduct.Price,
                Stock = updatedProduct.Stock,
                Category = updatedProduct.Category,
                MainImageUrl = updatedProduct.MainImageUrl,
                IsActive = updatedProduct.IsActive,
                CreatedAt = updatedProduct.CreatedAt,
                UpdatedAt = updatedProduct.UpdatedAt,
                Images = images.Select(img => new ProductImageDto
                {
                    Id = img.Id,
                    ProductId = img.ProductId,
                    ImageUrl = img.ImageUrl,
                    DisplayOrder = img.DisplayOrder,
                    IsMain = img.IsMain,
                    CreatedAt = img.CreatedAt
                }).ToList()
            };
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
                return false;

            // Elimina todas las imágenes del producto
            var images = await _productImageRepository.GetByProductIdAsync(id);
            foreach (var image in images)
            {
                if (!string.IsNullOrEmpty(image.ImageUrl) && !image.ImageUrl.StartsWith("http"))
                {
                    await _fileService.DeleteImageAsync(image.ImageUrl);
                }
                await _productImageRepository.DeleteAsync(image.Id);
            }

            return await _productRepository.DeleteAsync(id);
        }

        public async Task<IEnumerable<ProductListDto>> GetProductsByCategoryAsync(string category)
        {
            var products = await _productRepository.GetByCategoryAsync(category);
            return products.Select(p => new ProductListDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                Stock = p.Stock,
                Category = p.Category,
                MainImageUrl = p.MainImageUrl,
                IsActive = p.IsActive
            });
        }

        public async Task<IEnumerable<ProductListDto>> SearchProductsAsync(string searchTerm)
        {
            var products = await _productRepository.SearchAsync(searchTerm);
            return products.Select(p => new ProductListDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                Stock = p.Stock,
                Category = p.Category,
                MainImageUrl = p.MainImageUrl,
                IsActive = p.IsActive
            });
        }

        public async Task<IEnumerable<string>> GetCategoriesAsync()
        {
            return await _productRepository.GetCategoriesAsync();
        }
    }
}
