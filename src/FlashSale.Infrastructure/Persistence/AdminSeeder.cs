using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace FlashSale.Infrastructure.Persistence;

/// <summary>
/// Seed du lieu ban dau cho Role va User (ASP.NET Core Identity) vao PostgreSQLPrimary.
/// Idempotent: chi seed neu chua ton tai.
/// 
/// Thuat ngua chuan theo docs/domain-glossary.md: User, Role, PostgreSQLPrimary.
/// </summary>
public static class AdminSeeder
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Seed Role "Admin"
        const string adminRoleName = "Admin";
        if (!await roleManager.RoleExistsAsync(adminRoleName))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole<Guid>(adminRoleName));
            if (!roleResult.Succeeded)
            {
                var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Khong the seed Role '{adminRoleName}': {errors}");
            }
        }

        // Seed User "admin"
        const string adminUserName = "admin";
        const string adminEmail = "admin@gmail.com";
        const string adminPassword = "Admin@123"; // Placeholder, canh bao: can thay doi khi deploy production

        var adminUser = await userManager.FindByNameAsync(adminUserName);
        if (adminUser == null)
        {
            var user = new ApplicationUser(adminUserName, adminEmail);
            var createResult = await userManager.CreateAsync(user, adminPassword);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Khong the seed User '{adminUserName}': {errors}");
            }
            adminUser = user;
        }

        // Gan Role Admin cho User admin neu chua co
        if (!await userManager.IsInRoleAsync(adminUser, adminRoleName))
        {
            var addResult = await userManager.AddToRoleAsync(adminUser, adminRoleName);
            if (!addResult.Succeeded)
            {
                var errors = string.Join("; ", addResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Khong the gan Role '{adminRoleName}' cho User '{adminUserName}': {errors}");
            }
        }
    }
}
