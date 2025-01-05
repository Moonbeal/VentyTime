using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace VentyTime.Server.Services
{
    public class LocalStorageService : IStorageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly string _uploadDirectory;
        private readonly ILogger<LocalStorageService> _logger;

        public LocalStorageService(IWebHostEnvironment environment, ILogger<LocalStorageService> logger)
        {
            _environment = environment;
            _logger = logger;
            _uploadDirectory = Path.Combine(_environment.WebRootPath, "uploads");
            
            // Ensure upload directory exists
            if (!Directory.Exists(_uploadDirectory))
            {
                _logger.LogInformation("Creating upload directory: {Directory}", _uploadDirectory);
                Directory.CreateDirectory(_uploadDirectory);
            }
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            try
            {
                var filePath = Path.Combine(_uploadDirectory, fileName);
                var directory = Path.GetDirectoryName(filePath);
                
                _logger.LogInformation("Uploading file to {FilePath}", filePath);
                
                if (directory != null && !Directory.Exists(directory))
                {
                    _logger.LogInformation("Creating directory: {Directory}", directory);
                    Directory.CreateDirectory(directory);
                }

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await fileStream.CopyToAsync(stream);
                }

                _logger.LogInformation("File uploaded successfully: {FileName}", fileName);

                // Return the relative URL
                return $"/uploads/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file {FileName}", fileName);
                throw;
            }
        }

        public Task DeleteFileAsync(string fileName)
        {
            try
            {
                var filePath = Path.Combine(_uploadDirectory, fileName);
                _logger.LogInformation("Deleting file: {FilePath}", filePath);
                
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation("File deleted successfully: {FileName}", fileName);
                }
                else
                {
                    _logger.LogWarning("File not found: {FilePath}", filePath);
                }
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {FileName}", fileName);
                throw;
            }
        }
    }
}
