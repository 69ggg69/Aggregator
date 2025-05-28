using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Aggregator.Services
{
    public class ImageService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ImageService> _logger;
        private readonly string _imagesPath;

        public ImageService(IHttpClientFactory httpClientFactory, ILogger<ImageService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "Images");
            
            // Создаем папку для изображений, если её нет
            if (!Directory.Exists(_imagesPath))
            {
                Directory.CreateDirectory(_imagesPath);
            }
        }

        public async Task<string?> DownloadAndSaveImageAsync(string imageUrl, string shopName)
        {
            try
            {
                if (string.IsNullOrEmpty(imageUrl))
                    return null;

                var client = _httpClientFactory.CreateClient("SafeHttpClient");
                
                // Загружаем изображение
                var response = await client.GetAsync(imageUrl);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Не удалось загрузить изображение: {imageUrl}. Статус: {response.StatusCode}");
                    return null;
                }

                var imageBytes = await response.Content.ReadAsByteArrayAsync();
                
                // Определяем расширение файла
                var extension = GetImageExtension(imageUrl, response.Content.Headers.ContentType?.MediaType);
                
                // Генерируем уникальное имя файла
                var fileName = GenerateFileName(shopName, extension);
                var filePath = Path.Combine(_imagesPath, fileName);

                // Сохраняем файл
                await File.WriteAllBytesAsync(filePath, imageBytes);
                
                _logger.LogInformation($"Изображение сохранено: {fileName}");
                return fileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при загрузке изображения: {imageUrl}");
                return null;
            }
        }

        private string GetImageExtension(string url, string? contentType)
        {
            // Пытаемся определить расширение из URL
            var urlExtension = Path.GetExtension(url).ToLower();
            if (!string.IsNullOrEmpty(urlExtension) && IsValidImageExtension(urlExtension))
            {
                return urlExtension;
            }

            // Пытаемся определить расширение из Content-Type
            return contentType switch
            {
                "image/jpeg" => ".jpg",
                "image/jpg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                _ => ".jpg" // По умолчанию
            };
        }

        private bool IsValidImageExtension(string extension)
        {
            var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            return validExtensions.Contains(extension.ToLower());
        }

        private string GenerateFileName(string shopName, string extension)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var guid = Guid.NewGuid().ToString("N")[..8];
            var safeName = Regex.Replace(shopName, @"[^\w\-_]", "");
            return $"{safeName}_{timestamp}_{guid}{extension}";
        }
    }
} 