using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using VentyTime.Shared.Models;
using Microsoft.Extensions.Logging;

namespace VentyTime.Server.Services
{
    public interface ITokenService
    {
        Task<string> GenerateJwtToken(ApplicationUser user, UserRole selectedRole);
        Task<ClaimsPrincipal> ValidateToken(string token);
        bool IsTokenExpired(string token);
    }

    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TokenService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public TokenService(
            IConfiguration configuration,
            ILogger<TokenService> logger,
            UserManager<ApplicationUser> userManager)
        {
            _configuration = configuration;
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<string> GenerateJwtToken(ApplicationUser user, UserRole selectedRole)
        {
            try
            {
                _logger.LogInformation("Generating JWT token for user {Email} with role {Role}", 
                    user.Email, selectedRole);

                // Get user's actual roles
                var userRoles = await _userManager.GetRolesAsync(user);
                _logger.LogInformation("User {Email} has roles: {Roles}", user.Email, string.Join(", ", userRoles));

                // Verify that user has the selected role
                if (selectedRole == UserRole.Admin && !userRoles.Contains("Admin"))
                {
                    _logger.LogWarning("User {Email} attempted to generate token with Admin role without privileges", user.Email);
                    throw new UnauthorizedAccessException("User does not have admin privileges");
                }

                var claims = new List<Claim>
                {
                    new(JwtRegisteredClaimNames.Sub, user.Email!),
                    new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new(ClaimTypes.NameIdentifier, user.Id),
                    new(ClaimTypes.Name, user.Email!),
                    new(ClaimTypes.Email, user.Email!),
                    new("firstName", user.FirstName),
                    new("lastName", user.LastName),
                    new(ClaimTypes.Role, selectedRole.ToString())
                };

                _logger.LogInformation("Claims for user {Email}: {Claims}", 
                    user.Email, string.Join(", ", claims.Select(c => $"{c.Type}: {c.Value}")));

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                    _configuration["JwtSettings:SecretKey"] ?? throw new InvalidOperationException("JWT Key not configured")));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: _configuration["JwtSettings:Issuer"],
                    audience: _configuration["JwtSettings:Audience"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddDays(14),
                    signingCredentials: creds);

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                _logger.LogInformation("JWT token generated successfully for user {Email}", user.Email);
                return tokenString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating JWT token for user {Email}", user.Email);
                throw;
            }
        }

        public async Task<ClaimsPrincipal> ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]!);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = _configuration["JwtSettings:Issuer"],
                    ValidAudience = _configuration["JwtSettings:Audience"],
                    ClockSkew = TimeSpan.Zero
                };

                var principal = await Task.Run(() => tokenHandler.ValidateToken(token, validationParameters, out _));
                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                throw;
            }
        }

        public bool IsTokenExpired(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                return jwtToken.ValidTo < DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking token expiration");
                throw;
            }
        }
    }
}
