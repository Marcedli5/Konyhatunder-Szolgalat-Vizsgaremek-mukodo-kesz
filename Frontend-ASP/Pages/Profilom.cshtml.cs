using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Frontend_ASP.Pages
{
    [Authorize]
    public class ProfilomModel : PageModel
    {
        [BindProperty]
        public InputModel Input { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        public string Email { get; private set; } = string.Empty;

        public IActionResult OnGet()
        {
            Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            Input.Username = User.Identity?.Name ?? string.Empty;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, Input.Username.Trim()),
                new(ClaimTypes.Email, Email)
            };

            foreach (var role in User.FindAll(ClaimTypes.Role))
            {
                claims.Add(role);
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            StatusMessage = "A profil adatai frissültek.";
            return RedirectToPage();
        }

        public class InputModel
        {
            [Display(Name = "Felhasználónév")]
            [Required(ErrorMessage = "Add meg a felhasználónevet.")]
            [StringLength(80)]
            public string Username { get; set; } = string.Empty;

            [Display(Name = "Új jelszó")]
            [StringLength(100, ErrorMessage = "A jelszónak legalább {2} karakter hosszúnak kell lennie.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string? NewPassword { get; set; }

            [Display(Name = "Új jelszó megerősítése")]
            [DataType(DataType.Password)]
            [Compare("NewPassword", ErrorMessage = "A két jelszó nem egyezik.")]
            public string? ConfirmPassword { get; set; }
        }
    }
}
