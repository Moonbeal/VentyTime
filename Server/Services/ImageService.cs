using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace VentyTime.Server.Services
{
    public interface IImageService
    {
        Task<string> UploadImageAsync(Stream imageStream, string fileName);
        Task<bool> DeleteImageAsync(string imagePath);
    }

    public class ImageService : IImageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ImageService> _logger;
        private const int MaxWidth = 1920;
        private const int MaxHeight = 1080;
        private const int ThumbnailSize = 300;
        private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

        public ImageService(IWebHostEnvironment environment, ILogger<ImageService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<string> UploadImageAsync(Stream imageStream, string fileName)
        {
            try
            {
                // Validate file size
                if (imageStream.Length > MaxFileSize)
                {
                    throw new ArgumentException($"File size exceeds maximum limit of {MaxFileSize / 1024 / 1024}MB");
                }

                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                var thumbnailsFolder = Path.Combine(uploadsFolder, "thumbnails");

                // Ensure directories exist
                Directory.CreateDirectory(uploadsFolder);
                Directory.CreateDirectory(thumbnailsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                var thumbnailPath = Path.Combine(thumbnailsFolder, uniqueFileName);

                // Process and save the main image
                using (var memoryStream = new MemoryStream())
                {
                    await imageStream.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    using (var image = await Image.LoadAsync(memoryStream))
                    {
                        // Resize if necessary while maintaining aspect ratio
                        if (image.Width > MaxWidth || image.Height > MaxHeight)
                        {
                            image.Mutate(x => x.Resize(new ResizeOptions
                            {
                                Mode = ResizeMode.Max,
                                Size = new Size(MaxWidth, MaxHeight)
                            }));
                        }

                        // Optimize and save the main image
                        await image.SaveAsync(filePath);
                    }

                    // Reset position for thumbnail creation
                    memoryStream.Position = 0;

                    // Create and save thumbnail
                    using (var image = await Image.LoadAsync(memoryStream))
                    {
                        image.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Mode = ResizeMode.Crop,
                            Size = new Size(ThumbnailSize, ThumbnailSize)
                        }));
                        await image.SaveAsync(thumbnailPath);
                    }
                }

                return $"/uploads/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image");
                throw;
            }
        }

        public async Task<bool> DeleteImageAsync(string imagePath)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath))
                {
                    return false;
                }

                var fileName = Path.GetFileName(imagePath);
                var filePath = Path.Combine(_environment.WebRootPath, "uploads", fileName);
                var thumbnailPath = Path.Combine(_environment.WebRootPath, "uploads", "thumbnails", fileName);

                var success = false;

                if (File.Exists(filePath))
                {
                    await Task.Run(() => File.Delete(filePath));
                    success = true;
                }

                if (File.Exists(thumbnailPath))
                {
                    await Task.Run(() => File.Delete(thumbnailPath));
                    success = true;
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image");
                return false;
            }
        }
    }
}
