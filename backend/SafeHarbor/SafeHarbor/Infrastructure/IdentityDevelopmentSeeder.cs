using Microsoft.AspNetCore.Identity;
using SafeHarbor.Auth;

namespace SafeHarbor.Infrastructure;

public static class IdentityDevelopmentSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        await EnsureRoleAsync(roleManager, "Admin");
        await EnsureRoleAsync(roleManager, "SocialWorker");
        await EnsureRoleAsync(roleManager, "Donor");

        // NOTE: These local accounts intentionally exist only for Development + LocalAuth enabled mode.
        // They provide deterministic credentials for demos/tests without introducing staging/prod backdoors.
        await EnsureUserAsync(userManager, "alice@example.com", "Password123!Aa", "Donor");
        await EnsureUserAsync(userManager, "bob@example.com", "Password123!Aa", "Donor");
        await EnsureUserAsync(userManager, "admin@safeharbor.local", "Password123!Aa", "Admin");
        await EnsureUserAsync(userManager, "socialworker@safeharbor.local", "Password123!Aa", "SocialWorker");
    }

    private static async Task EnsureRoleAsync(RoleManager<IdentityRole<Guid>> roleManager, string roleName)
    {
        if (await roleManager.RoleExistsAsync(roleName))
        {
            return;
        }

        var createResult = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Failed to create identity role '{roleName}': {errors}");
        }
    }

    private static async Task EnsureUserAsync(UserManager<AppUser> userManager, string email, string password, string roleName)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Failed to seed identity user '{email}': {errors}");
            }
        }

        if (await userManager.IsInRoleAsync(user, roleName))
        {
            return;
        }

        var roleResult = await userManager.AddToRoleAsync(user, roleName);
        if (!roleResult.Succeeded)
        {
            var errors = string.Join("; ", roleResult.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Failed to assign role '{roleName}' to '{email}': {errors}");
        }
    }
}
