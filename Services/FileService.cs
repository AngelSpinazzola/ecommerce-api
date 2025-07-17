using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace EcommerceAPI.Services
{
    public interface IFileService
    {
        Task<string> SaveImageAsync(IFormFile imageFile, string folder = "products");
        Task<bool> DeleteImageAsync(string imagePath);
        bool IsValidImageFile(IFormFile file);
        Task<List<string>> SaveMultipleImagesAsync(IFormFile[] imageFiles, string folder = "products");
    }

    public class FileService : IFileService
    {
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<FileService> _logger;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long MaxFileSize = 10 * 1024 * 1024; // 10MB (límite de Cloudinary free)

        public FileService(IConfiguration configuration, ILogger<FileService> logger)
        {
            _logger = logger;

            var account = new Account(
                configuration["Cloudinary:CloudName"],
                configuration["Cloudinary:ApiKey"],
                configuration["Cloudinary:ApiSecret"]
            );

            _cloudinary = new Cloudinary(account);
        }

        public async Task<string> SaveImageAsync(IFormFile imageFile, string folder = "products")
        {
            try
            {
                if (!IsValidImageFile(imageFile))
                {
                    throw new ArgumentException("Archivo de imagen no válido");
                }

                using var stream = imageFile.OpenReadStream();

                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(imageFile.FileName, stream),
                    Folder = folder,
                    UseFilename = false,
                    UniqueFilename = true,
                    Overwrite = false,
                    Transformation = new Transformation()
                        .Quality("auto")
                        .FetchFormat("auto")
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.Error != null)
                {
                    _logger.LogError($"Error al subir imagen a Cloudinary: {uploadResult.Error.Message}");
                    throw new Exception($"Error al subir imagen: {uploadResult.Error.Message}");
                }

                _logger.LogInformation($"Imagen subida exitosamente: {uploadResult.SecureUrl}");
                return uploadResult.SecureUrl.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en SaveImageAsync");
                throw;
            }
        }

        public async Task<bool> DeleteImageAsync(string imagePath)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath))
                    return false;

                // Solo procesar URLs de Cloudinary
                if (!imagePath.Contains("cloudinary.com"))
                    return true; // No es de Cloudinary, no hacer nada

                var publicId = ExtractPublicIdFromUrl(imagePath);
                if (string.IsNullOrEmpty(publicId))
                    return false;

                var deletionParams = new DeletionParams(publicId);
                var result = await _cloudinary.DestroyAsync(deletionParams);

                _logger.LogInformation($"Imagen eliminada: {publicId}, Resultado: {result.Result}");
                return result.Result == "ok";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar imagen: {imagePath}");
                return false;
            }
        }

        public bool IsValidImageFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;

            if (file.Length > MaxFileSize)
                return false;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
                return false;

            // Validar MIME type
            var allowedMimeTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
            return allowedMimeTypes.Contains(file.ContentType.ToLower());
        }

        public async Task<List<string>> SaveMultipleImagesAsync(IFormFile[] imageFiles, string folder = "products")
        {
            var uploadedUrls = new List<string>();

            foreach (var file in imageFiles)
            {
                try
                {
                    var url = await SaveImageAsync(file, folder);
                    uploadedUrls.Add(url);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error al subir archivo: {file.FileName}");
                    // Continúa con los otros archivos
                }
            }

            return uploadedUrls;
        }

        private string ExtractPublicIdFromUrl(string imageUrl)
        {
            try
            {
                // URL típica de Cloudinary: 
                // https://res.cloudinary.com/cloudname/image/upload/v1234567890/folder/filename.jpg
                var uri = new Uri(imageUrl);
                var pathParts = uri.AbsolutePath.Split('/');

                // Buscar la parte después de /upload/
                var uploadIndex = Array.IndexOf(pathParts, "upload");
                if (uploadIndex >= 0 && uploadIndex + 2 < pathParts.Length)
                {
                    // Saltar /upload/ y versión (v1234567890)
                    var publicIdParts = pathParts.Skip(uploadIndex + 2).ToArray();
                    var publicId = string.Join("/", publicIdParts);

                    // Remover extensión
                    var lastDotIndex = publicId.LastIndexOf('.');
                    if (lastDotIndex > 0)
                        publicId = publicId.Substring(0, lastDotIndex);

                    return publicId;
                }

                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}