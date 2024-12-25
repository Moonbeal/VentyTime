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
                    return BadRequest(new { errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
                }

                var existingUser = await _userService.GetUserByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    return BadRequest(new { message = "Email already registered" });
                }

                var (succeeded, errors) = await _userService.CreateUserAsync(model);
                if (succeeded)
                {
                    var user = await _userService.GetUserByEmailAsync(model.Email);
                    var token = await _tokenService.GenerateJwtToken(user!);

                    _logger.LogInformation("User registered successfully: {Email}", model.Email);

                    return Ok(new AuthResponse
                    {
                        Token = token,
                        User = new UserDto
                        {
                            Id = user!.Id,
                            Email = user.Email!,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            AvatarUrl = user.AvatarUrl,
                            Role = UserRole.User
                        }
                    });
                }

                return BadRequest(new { errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for email: {Email}", model.Email);
                return StatusCode(500, new { message = "An error occurred during registration" });
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            try
            {
                _logger.LogInformation("Login attempt for email: {Email}", model.Email);

                if (!ModelState.IsValid)
                {
                    return BadRequest(new { errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
                }

                var user = await _userService.GetUserByEmailAsync(model.Email);
                if (user == null)
                {
                    _logger.LogWarning("Login failed - user not found: {Email}", model.Email);
                    return BadRequest(new { message = "Invalid email or password" });
                }

                if (!user.IsActive)
                {
                    _logger.LogWarning("Login attempt for deactivated account: {Email}", model.Email);
                    return BadRequest(new { message = "Your account has been deactivated" });
                }

                if (!await _userService.ValidateUserAsync(model))
                {
                    _logger.LogWarning("Invalid password attempt for user: {Email}", model.Email);
                    return BadRequest(new { message = "Invalid email or password" });
                }

                // Перевіряємо роль
                if (model.Role != UserRole.None)
                {
                    var isInRole = await _userService.IsInRoleAsync(user, model.Role.ToString());
                    if (!isInRole)
                    {
                        _logger.LogWarning("User {Email} attempted to login with unauthorized role: {Role}", model.Email, model.Role);
                        return BadRequest(new { message = "Unauthorized role access" });
                    }
                }

                // Оновлюємо інформацію про останній вхід
                await _userService.UpdateUserLastLoginAsync(user);

                var token = await _tokenService.GenerateJwtToken(user);
                _logger.LogInformation("User {Email} logged in successfully", model.Email);

                return Ok(new AuthResponse
                {
                    Token = token,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email!,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        AvatarUrl = user.AvatarUrl,
                        Role = model.Role
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", model.Email);
                return StatusCode(500, new { message = "An error occurred during login" });
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
                return StatusCode(500, new { message = "An error occurred during logout" });
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
                return StatusCode(500, new { message = "An error occurred while getting user information" });
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
                return StatusCode(500, new { message = "An error occurred while refreshing token" });
            }
        }
    }
}
