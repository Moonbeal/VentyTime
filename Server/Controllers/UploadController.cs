using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using VentyTime.Server.Services;
using Microsoft.AspNetCore.Http;

namespace VentyTime.Server.Controllers
{
    public class ImageUploadResult
    {
        public string ImageUrl { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {
        private readonly ILogger<UploadController> _logger;
        private readonly IImageService _imageService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UploadController(
            ILogger<UploadController> logger,
            IImageService imageService,
            IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _imageService = imageService;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpPost]
        [RequestSizeLimit(10 * 1024 * 1024)] // 10MB
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<ActionResult<ImageUploadResult>> Upload([FromForm] IFormFile file)
        {
            try
            {
                _logger.LogInformation("Upload request received from user: {User}", User.Identity?.Name);

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
                    _logger.LogWarning("Invalid file extension: {Extension}", extension);
                    return BadRequest($"Invalid file type. Allowed types are: {string.Join(", ", allowedExtensions)}");
                }

                await using var stream = file.OpenReadStream();
                var relativePath = await _imageService.UploadImageAsync(stream, file.FileName);
                
                // Get the base URL
                var request = _httpContextAccessor.HttpContext?.Request;
                var baseUrl = $"{request?.Scheme}://{request?.Host}";
                
                // Create full URLs
                var imageUrl = $"{baseUrl}{relativePath}";
                var thumbnailUrl = imageUrl.Replace("/uploads/", "/uploads/thumbnails/");
                
                _logger.LogInformation("Image uploaded successfully. URL: {ImageUrl}", imageUrl);
                
                return Ok(new ImageUploadResult 
                { 
                    ImageUrl = imageUrl,
                    ThumbnailUrl = thumbnailUrl
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error during upload");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                return StatusCode(500, "An error occurred while uploading the file");
            }
        }
    }
}
