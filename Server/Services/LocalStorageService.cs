using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace VentyTime.Server.Services
{
    public class LocalStorageService : IStorageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly string _uploadDirectory;

        public LocalStorageService(IWebHostEnvironment environment)
        {
            _environment = environment;
            _uploadDirectory = Path.Combine(_environment.WebRootPath, "uploads");
            
            // Ensure upload directory exists
            if (!Directory.Exists(_uploadDirectory))
            {
                Directory.CreateDirectory(_uploadDirectory);
            }
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            var filePath = Path.Combine(_uploadDirectory, fileName);
            var directory = Path.GetDirectoryName(filePath);
            
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await fileStream.CopyToAsync(stream);
            }

            // Return the relative URL
            return $"/uploads/{fileName}";
        }

        public Task DeleteFileAsync(string fileName)
        {
            var filePath = Path.Combine(_uploadDirectory, fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            return Task.CompletedTask;
        }
    }
}
