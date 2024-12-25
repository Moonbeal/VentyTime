using Microsoft.AspNetCore.Identity;
using VentyTime.Shared.Models;

namespace VentyTime.Server.Data
{
    public static class SeedData
    {
        public static async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            var roles = Enum.GetNames(typeof(UserRole));
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }
    }
}
