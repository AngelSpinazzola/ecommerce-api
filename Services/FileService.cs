namespace EcommerceAPI.Services
{
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileService> _logger;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

        public FileService(IWebHostEnvironment environment, ILogger<FileService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<string> SaveImageAsync(IFormFile imageFile, string folder = "products")
        {
            try
            {
                if (!IsValidImageFile(imageFile))
                {
                    throw new ArgumentException("Invalid image file");
                }

                // Crea carpeta si no existe
                var uploadsDir = Path.Combine(_environment.WebRootPath, "images", folder);
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                // Genera nombre único
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
                var filePath = Path.Combine(uploadsDir, fileName);

                // Guarda archivo
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                // Retorna URL relativa
                return $"/images/{folder}/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving image file");
                throw;
            }
        }

        public async Task<bool> DeleteImageAsync(string imagePath)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath) || imagePath.StartsWith("http"))
                    return true; // No es archivo local

                var fullPath = Path.Combine(_environment.WebRootPath, imagePath.TrimStart('/'));

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image file: {ImagePath}", imagePath);
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
            return _allowedExtensions.Contains(extension);
        }
    }
}
