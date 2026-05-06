using Microsoft.AspNetCore.Authentication.Cookies;
using Frontend_ASP.Services;

namespace Frontend_ASP
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(2);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            builder.Services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Identity/Account/Login";
                    options.LogoutPath = "/Identity/Account/Logout";
                    options.AccessDeniedPath = "/Identity/Account/Login";
                });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy =>
                    policy.RequireAssertion(context =>
                        context.User.IsInRole("Admin") ||
                        context.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value.StartsWith("admin", StringComparison.OrdinalIgnoreCase) == true));
            });
            builder.Services.AddRazorPages();
            builder.Services.AddControllers();

            builder.Services.AddHttpClient<BackendApiClient>(client =>
            {
                var baseUrl = builder.Configuration["BackendApi:BaseUrl"]
                    ?? throw new InvalidOperationException("BackendApi:BaseUrl nincs beallitva.");

                client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
            });

            builder.Services.AddScoped<LegacyMenuService>();
            builder.Services.AddScoped<CartService>();
            builder.Services.AddScoped<LegacyOrderService>();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseSession();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllers();
            app.MapRazorPages()
               .WithStaticAssets();

            await app.RunAsync();
        }
    }
}
