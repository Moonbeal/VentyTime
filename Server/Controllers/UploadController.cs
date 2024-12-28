using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using System.Security.AccessControl;

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

        [HttpPost("UploadImage")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = 5242880)] // 5MB
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<IActionResult> Upload([FromForm] IFormFile file)
        {
            try
            {
                _logger.LogInformation("Upload request received from user: {User}", User.Identity?.Name);
                _logger.LogInformation("User roles: {Roles}", string.Join(", ", User.Claims.Where(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role").Select(c => c.Value)));
                _logger.LogInformation("Upload request received for file: {FileName}, ContentType: {ContentType}, Length: {Length}, WebRootPath: {WebRootPath}", 
                    file?.FileName, file?.ContentType, file?.Length, _environment.WebRootPath);

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

                // Ensure uploads directory exists
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    _logger.LogInformation("Creating uploads directory: {UploadsFolder}", uploadsFolder);
                    Directory.CreateDirectory(uploadsFolder);
                    
                    // Ensure directory has proper permissions
                    try 
                    {
                        var directoryInfo = new DirectoryInfo(uploadsFolder);
                        var security = directoryInfo.GetAccessControl();
                        security.AddAccessRule(new FileSystemAccessRule(
                            "Everyone",
                            FileSystemRights.Modify,
                            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                            PropagationFlags.None,
                            AccessControlType.Allow));
                        directoryInfo.SetAccessControl(security);
                        _logger.LogInformation("Set permissions on uploads directory");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to set directory permissions, but continuing");
                    }
                }

                // Generate unique filename
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                _logger.LogInformation("Saving file to: {FilePath}", filePath);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _logger.LogInformation("File saved successfully: {FileName}", uniqueFileName);

                // Return the URL path that will work with our static files middleware
                var url = $"/uploads/{uniqueFileName}";
                _logger.LogInformation("Generated URL: {Url}", url);

                // Set content type and caching headers
                Response.Headers.Add("Content-Type", GetContentType(extension));
                Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
                
                return Ok(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file: {Message}", ex.Message);
                
                // Check for specific error types
                if (ex is UnauthorizedAccessException)
                {
                    return StatusCode(500, "Server error: Unable to access upload directory. Please contact administrator.");
                }
                else if (ex is IOException)
                {
                    return StatusCode(500, "Server error: Unable to write file. Please try again.");
                }
                else if (ex is System.Security.SecurityException)
                {
                    return StatusCode(500, "Server error: Permission denied. Please contact administrator.");
                }
                
                return StatusCode(500, "Server error: Unable to process upload. Please try again.");
            }
        }

        private static string GetContentType(string extension)
        {
            return extension.ToLower() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                _ => "application/octet-stream"
            };
        }
    }
}
