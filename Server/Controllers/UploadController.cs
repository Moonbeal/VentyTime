using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace VentyTime.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<UploadController> _logger;

        public UploadController(IWebHostEnvironment environment, ILogger<UploadController> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        [HttpPost]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = 5242880)] // 5MB
        public async Task<IActionResult> Upload([FromForm] IFormFile file)
        {
            try
            {
                _logger.LogInformation("Upload request received");

                if (file == null || file.Length == 0)
                {
                    _logger.LogWarning("No file was uploaded");
                    return BadRequest("No file was uploaded");
                }

                // Validate file extension
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    _logger.LogWarning($"Invalid file extension: {extension}");
                    return BadRequest($"Invalid file type. Allowed types are: {string.Join(", ", allowedExtensions)}");
                }

                // Ensure uploads directory exists
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    _logger.LogInformation("Creating uploads directory");
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generate unique filename
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                _logger.LogInformation($"Saving file to: {filePath}");
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _logger.LogInformation($"File saved successfully: {uniqueFileName}");

                // Return the relative URL without quotes
                var url = $"/uploads/{uniqueFileName}";
                _logger.LogInformation($"File uploaded successfully: {url}");
                return Ok(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
