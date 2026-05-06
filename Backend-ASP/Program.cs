
using Backend_ASP.Data;
using Backend_ASP.Services;
using Konyhatunder_Szolgalat_Vizsgaremek.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Backend_ASP
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            var legacyMySqlConnectionString = builder.Configuration.GetConnectionString("LegacyMySql")
                ?? "server=localhost;user=root;database=vizsgaremek_etlap";

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString)
                    .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning)));

            builder.Services.AddDbContext<VizsgaremekEtlapContext>(options =>
                options.UseMySql(
                    legacyMySqlConnectionString,
                    ServerVersion.Parse("10.4.32-mariadb"),
                    mySqlOptions => mySqlOptions.EnableRetryOnFailure()));

            builder.Services.AddDatabaseDeveloperPageExceptionFilter();
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(2);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            builder.Services.AddDefaultIdentity<IdentityUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
            })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            builder.Services.AddControllers();
            builder.Services.AddOpenApi();

            builder.Services.AddScoped<LegacyMenuService>();
            builder.Services.AddScoped<LegacyUserLinkService>();
            builder.Services.AddScoped<CartService>();
            builder.Services.AddScoped<LegacyOrderService>();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
               app.MapOpenApi();

                using var scope = app.Services.CreateScope();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseSession();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();


            

               using(var scope = app.Services.CreateScope())
               {
                   await DatabaseInitializer.InitializeAsync(scope.ServiceProvider);
                    
                   await LegacyPortalInitializer.InitializeAsync(scope.ServiceProvider);
               }

            await app.RunAsync();
        }
    }
}
