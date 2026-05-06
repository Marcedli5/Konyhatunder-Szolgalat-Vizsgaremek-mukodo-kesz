using Backend_ASP.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Backend_ASP.Data
{
    public static class DatabaseInitializer
    {
        private const string AdminRoleName = "Admin";
        private const string AdminEmail = "admin@admin.com";
        private const string AdminPassword = "admin123";

        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

            var userTableExists = await context.Database
    .SqlQueryRaw<int>(@"
        SELECT CASE 
            WHEN EXISTS (
                SELECT 1 
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_TYPE = 'BASE TABLE'
            )
            THEN 1 ELSE 0
        END AS Value")
    .SingleAsync() == 1;

            if (!userTableExists)
            {
                await context.Database.MigrateAsync();
                await EnsureRoleAsync(roleManager);
                await EnsureAdminUserAsync(userManager);
                await EnsureAdminInRoleAsync(userManager);
                await SeedMenuItemsAsync(context);
            }


            



        }

        private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager)
        {
            if (!await roleManager.RoleExistsAsync(AdminRoleName))
            {
                await roleManager.CreateAsync(new IdentityRole(AdminRoleName));
            }
        }

        private static async Task EnsureAdminUserAsync(UserManager<IdentityUser> userManager)
        {
            for (var attempt = 1; attempt <= 3; attempt++)
            {
                var result = await TryEnsureAdminUserAsync(userManager);
                if (result is null)
                {
                    return;
                }

                if (!IsConcurrencyFailure(result) || attempt == 3)
                {
                    throw new InvalidOperationException($"Az admin felhasznalo frissitese sikertelen: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }

        private static async Task<IdentityResult?> TryEnsureAdminUserAsync(UserManager<IdentityUser> userManager)
        {
            var adminUser = await userManager.FindByEmailAsync(AdminEmail);

            if (adminUser is null)
            {
                adminUser = new IdentityUser
                {
                    UserName = AdminEmail,
                    Email = AdminEmail,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(adminUser, AdminPassword);
                return createResult.Succeeded || IsDuplicateUserFailure(createResult)
                    ? null
                    : createResult;
            }

            if (adminUser.UserName != AdminEmail || adminUser.Email != AdminEmail || !adminUser.EmailConfirmed)
            {
                adminUser.UserName = AdminEmail;
                adminUser.Email = AdminEmail;
                adminUser.EmailConfirmed = true;

                var updateResult = await userManager.UpdateAsync(adminUser);
                if (!updateResult.Succeeded)
                {
                    return updateResult;
                }
            }

            if (await userManager.CheckPasswordAsync(adminUser, AdminPassword))
            {
                return null;
            }

            var passwordResult = await userManager.HasPasswordAsync(adminUser)
                ? await userManager.ResetPasswordAsync(adminUser, await userManager.GeneratePasswordResetTokenAsync(adminUser), AdminPassword)
                : await userManager.AddPasswordAsync(adminUser, AdminPassword);

            return passwordResult.Succeeded ? null : passwordResult;
        }

        private static bool IsConcurrencyFailure(IdentityResult result)
        {
            return result.Errors.Any(error => error.Code == nameof(IdentityErrorDescriber.ConcurrencyFailure));
        }

        private static bool IsDuplicateUserFailure(IdentityResult result)
        {
            return result.Errors.All(error =>
                error.Code == nameof(IdentityErrorDescriber.DuplicateUserName) ||
                error.Code == nameof(IdentityErrorDescriber.DuplicateEmail));
        }
        private static async Task EnsureAdminInRoleAsync(UserManager<IdentityUser> userManager)
        {
            var adminUser = await userManager.FindByEmailAsync(AdminEmail)
                ?? throw new InvalidOperationException("Az admin felhasználó nem található.");

            if (!await userManager.IsInRoleAsync(adminUser, AdminRoleName))
            {
                var addToRoleResult = await userManager.AddToRoleAsync(adminUser, AdminRoleName);
                if (!addToRoleResult.Succeeded)
                {
                    throw new InvalidOperationException($"Az admin szerepkör hozzárendelése sikertelen: {string.Join(", ", addToRoleResult.Errors.Select(e => e.Description))}");
                }
            }
        }

        private static async Task SeedMenuItemsAsync(ApplicationDbContext context)
        {
            if (await context.MenuItems.AnyAsync())
            {
                return;
            }

            var items = new[]
            {
                new MenuItem { Category = "Levesek", Name = "Tyúkhúsleves gazdagon", Description = "Aranyló húsleves sok zöldséggel és cérnametélttel.", Price = 1490, IsAvailable = true, DisplayOrder = 1 },
                new MenuItem { Category = "Levesek", Name = "Gulyásleves csipetkével", Description = "Hagyományos, laktató magyar leves.", Price = 1790, IsAvailable = true, DisplayOrder = 2 },
                new MenuItem { Category = "Főételek", Name = "Rántott csirkemell párolt rizzsel", Description = "Klasszikus ropogós bundában, friss körettel.", Price = 2690, IsAvailable = true, DisplayOrder = 1 },
                new MenuItem { Category = "Főételek", Name = "Brassói aprópecsenye", Description = "Szaftos sertéshús fokhagymás burgonyával.", Price = 2890, IsAvailable = true, DisplayOrder = 2 },
                new MenuItem { Category = "Vegetáriánus", Name = "Sajtos-tejfölös tészta", Description = "Krémes, gyors kedvenc könnyed ebédhez.", Price = 1990, IsAvailable = true, DisplayOrder = 1 },
                new MenuItem { Category = "Desszertek", Name = "Somlói galuska", Description = "Puha piskóta csokoládéöntettel és tejszínhabbal.", Price = 1190, IsAvailable = true, DisplayOrder = 1 }
            };

            context.MenuItems.AddRange(items);
            await context.SaveChangesAsync();
        }
    }
}
