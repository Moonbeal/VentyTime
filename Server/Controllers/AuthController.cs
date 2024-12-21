using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using VentyTime.Shared.Models;
using VentyTime.Shared.Models.Auth;
using Microsoft.AspNetCore.Authorization;

namespace VentyTime.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest model)
        {
            try
            {
                _logger.LogInformation("Starting user registration process for email: {Email}", model.Email);

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state during registration");
                    return BadRequest(ModelState);
                }

                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning("User with email {Email} already exists", model.Email);
                    return BadRequest(new AuthResponse(false, "User with this email already exists"));
                }

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    CreatedAt = DateTime.UtcNow,
                    Role = model.Role
                };

                _logger.LogInformation("Creating new user with email: {Email}", model.Email);
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User {Email} created successfully", model.Email);

                    // Add user to role
                    await _userManager.AddToRoleAsync(user, model.Role.ToString());
                    _logger.LogInformation("Added user {Email} to role: {Role}", model.Email, model.Role);

                    // Generate JWT token
                    var token = GenerateJwtToken(user);
                    _logger.LogInformation("JWT token generated for user {Email}", model.Email);

                    return Ok(new AuthResponse
                    {
                        Token = token,
                        UserId = user.Id,
                        Username = user.UserName ?? string.Empty,
                        Email = user.Email ?? string.Empty,
                        Role = user.Role,
                        LastLoginAt = DateTime.UtcNow,
                        Success = true,
                        Message = "Registration successful"
                    });
                }

                _logger.LogError("Failed to create user {Email}. Errors: {Errors}", 
                    model.Email, 
                    string.Join(", ", result.Errors));

                return BadRequest(new AuthResponse(false, 
                    "Registration failed: " + string.Join(", ", result.Errors.Select(e => e.Description))));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during registration");
                return StatusCode(500, new AuthResponse(false, "An unexpected error occurred"));
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest model)
        {
            try
            {
                _logger.LogInformation("Starting login process for email: {Email}", model.Email);

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state during login");
                    return BadRequest(ModelState);
                }

                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    _logger.LogWarning("User with email {Email} not found", model.Email);
                    return Unauthorized(new AuthResponse(false, "Invalid email or password"));
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User {Email} logged in successfully", model.Email);

                    // Update last login time
                    user.LastLoginAt = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);

                    // Generate JWT token
                    var token = GenerateJwtToken(user);

                    return Ok(new AuthResponse
                    {
                        Token = token,
                        UserId = user.Id,
                        Username = user.UserName ?? string.Empty,
                        Email = user.Email ?? string.Empty,
                        Role = user.Role,
                        LastLoginAt = user.LastLoginAt ?? DateTime.UtcNow,
                        Success = true,
                        Message = "Login successful"
                    });
                }

                _logger.LogWarning("Invalid password for user {Email}", model.Email);
                return Unauthorized(new AuthResponse(false, "Invalid email or password"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during login");
                return StatusCode(500, new AuthResponse(false, "An unexpected error occurred"));
            }
        }

        private string GenerateJwtToken(ApplicationUser user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.UserName ?? string.Empty),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new(ClaimTypes.Role, user.Role.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"] ?? throw new InvalidOperationException()));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddDays(Convert.ToDouble(_configuration["JwtSettings:ExpirationInDays"]));

            var token = new JwtSecurityToken(
                _configuration["JwtSettings:Issuer"],
                _configuration["JwtSettings:Audience"],
                claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
