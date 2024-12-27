using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VentyTime.Server.Services;
using VentyTime.Shared.Models;
using VentyTime.Shared.Models.Auth;

namespace VentyTime.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IUserService userService,
            ITokenService tokenService,
            ILogger<AuthController> logger)
        {
            _userService = userService;
            _tokenService = tokenService;
            _logger = logger;
        }

        [HttpPost]
        [Route("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest model)
        {
            try
            {
                _logger.LogInformation("Register attempt for email: {Email}", model.Email);

                if (!ModelState.IsValid)
                {
                    var modelErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    _logger.LogWarning("Invalid model state for registration: {Errors}", string.Join(", ", modelErrors));
                    return BadRequest(new { errors = modelErrors });
                }

                var existingUser = await _userService.GetUserByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning("Registration failed - email already exists: {Email}", model.Email);
                    return BadRequest(new { message = "Email already registered" });
                }

                _logger.LogInformation("Creating user with email: {Email}", model.Email);
                var (succeeded, errors) = await _userService.CreateUserAsync(model);
                
                if (!succeeded)
                {
                    _logger.LogWarning("User creation failed for {Email}: {Errors}", model.Email, string.Join(", ", errors));
                    return BadRequest(new { errors });
                }

                _logger.LogInformation("User created successfully, retrieving user details: {Email}", model.Email);
                var user = await _userService.GetUserByEmailAsync(model.Email);
                if (user == null)
                {
                    _logger.LogError("User was created but could not be retrieved: {Email}", model.Email);
                    return StatusCode(500, new { message = "User was created but could not be retrieved" });
                }

                _logger.LogInformation("Generating JWT token for user: {Email}", model.Email);
                var token = await _tokenService.GenerateJwtToken(user);
                
                _logger.LogInformation("User registered successfully: {Email}", model.Email);
                return Ok(new AuthResponse
                {
                    Success = true,
                    Token = token,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email!,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        AvatarUrl = user.AvatarUrl,
                        Role = UserRole.User
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for email: {Email}. Error: {Error}", 
                    model.Email, ex.ToString());
                
                return StatusCode(500, new { 
                    message = "An error occurred during registration",
                    error = ex.Message,
                    details = ex.ToString(),
                    stackTrace = ex.StackTrace,
                    innerException = ex.InnerException?.ToString()
                });
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            try
            {
                _logger.LogInformation("Login attempt for email: {Email} with role: {Role}", 
                    model.Email, model.SelectedRole);

                if (!ModelState.IsValid)
                {
                    var modelErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    _logger.LogWarning("Invalid model state for login: {Errors}", string.Join(", ", modelErrors));
                    return BadRequest(new { errors = modelErrors });
                }

                var isValid = await _userService.ValidateUserAsync(model);
                if (!isValid)
                {
                    _logger.LogWarning("Login failed for {Email} with role {Role}", 
                        model.Email, model.SelectedRole);
                    return BadRequest(new { message = "Invalid email, password, or selected role" });
                }

                var user = await _userService.GetUserByEmailAsync(model.Email);
                if (user == null)
                {
                    _logger.LogError("User was validated but could not be retrieved: {Email}", model.Email);
                    return StatusCode(500, new { message = "An error occurred during login" });
                }

                var token = await _tokenService.GenerateJwtToken(user);
                _logger.LogInformation("User logged in successfully: {Email} with role {Role}", 
                    model.Email, model.SelectedRole);

                return Ok(new AuthResponse
                {
                    Success = true,
                    Token = token,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email!,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        AvatarUrl = user.AvatarUrl,
                        Role = model.SelectedRole
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", model.Email);
                var response = new { 
                    message = "An error occurred during login", 
                    error = ex.Message,
                    details = ex.ToString(),
                    stackTrace = ex.StackTrace
                };
                return StatusCode(500, response);
            }
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    _logger.LogInformation("User {UserId} logged out", userId);
                    await Task.CompletedTask; 
                }
                return Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                var response = new { 
                    message = "An error occurred during logout", 
                    error = ex.Message,
                    details = ex.ToString(),
                    stackTrace = ex.StackTrace
                };
                return StatusCode(500, response);
            }
        }

        [Authorize]
        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var user = await _userService.GetUserByEmailAsync(User.FindFirst(ClaimTypes.Email)?.Value!);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var roles = await _userService.GetUserRolesAsync(user);
                var role = roles.FirstOrDefault();
                var userRole = Enum.TryParse<UserRole>(role, out var parsedRole) ? parsedRole : UserRole.None;

                return Ok(new UserDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    AvatarUrl = user.AvatarUrl,
                    Role = userRole
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                var response = new { 
                    message = "An error occurred while getting user information", 
                    error = ex.Message,
                    details = ex.ToString(),
                    stackTrace = ex.StackTrace
                };
                return StatusCode(500, response);
            }
        }

        [Authorize]
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken()
        {
            try
            {
                var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                if (string.IsNullOrEmpty(token))
                {
                    return BadRequest(new { message = "No token provided" });
                }

                if (_tokenService.IsTokenExpired(token))
                {
                    return Unauthorized(new { message = "Token expired" });
                }

                var principal = await _tokenService.ValidateToken(token);
                var email = principal.FindFirst(ClaimTypes.Email)?.Value;
                
                if (string.IsNullOrEmpty(email))
                {
                    return BadRequest(new { message = "Invalid token" });
                }

                var user = await _userService.GetUserByEmailAsync(email);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var newToken = await _tokenService.GenerateJwtToken(user);
                return Ok(new { token = newToken });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                var response = new { 
                    message = "An error occurred while refreshing token", 
                    error = ex.Message,
                    details = ex.ToString(),
                    stackTrace = ex.StackTrace
                };
                return StatusCode(500, response);
            }
        }
    }
}
