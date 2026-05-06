using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Frontend_ASP.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel(ILogger<RegisterModel> logger) : PageModel
    {
        private readonly ILogger<RegisterModel> _logger = logger;

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? FormMessage { get; private set; }

        public void OnGet(string? returnUrl = null, string? email = null)
        {
            Input.ReturnUrl = returnUrl;
            if (!string.IsNullOrWhiteSpace(email))
            {
                Input.Email = email;
            }
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            Input.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, Input.Username.Trim()),
                new(ClaimTypes.Email, Input.Email.Trim()),
                new(ClaimTypes.MobilePhone, Input.PhoneNumber.Trim()),
                new(ClaimTypes.StreetAddress, Input.Address.Trim())
            };

            if (Input.Email.StartsWith("admin", StringComparison.OrdinalIgnoreCase))
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            _logger.LogInformation("Frontend cookie user registered.");
            return LocalRedirect(returnUrl);
        }

        public class InputModel
        {
            [Display(Name = "Felhasználónév")]
            [Required(ErrorMessage = "Add meg a felhasználónevet.")]
            [StringLength(80, ErrorMessage = "Legfeljebb 80 karakter lehet.")]
            public string Username { get; set; } = string.Empty;

            [Display(Name = "E-mail cím")]
            [Required(ErrorMessage = "Add meg az e-mail címet.")]
            [EmailAddress(ErrorMessage = "Érvényes e-mail címet adj meg.")]
            public string Email { get; set; } = string.Empty;

            [Display(Name = "Telefonszám")]
            [Required(ErrorMessage = "Add meg a telefonszámot.")]
            [Phone(ErrorMessage = "Érvényes telefonszámot adj meg.")]
            [StringLength(30, ErrorMessage = "Legfeljebb 30 karakter lehet.")]
            public string PhoneNumber { get; set; } = string.Empty;

            [Display(Name = "Lakcím")]
            [Required(ErrorMessage = "Add meg a lakcímet.")]
            [StringLength(140, ErrorMessage = "Legfeljebb 140 karakter lehet.")]
            public string Address { get; set; } = string.Empty;

            [Display(Name = "Jelszó")]
            [Required(ErrorMessage = "Add meg a jelszót.")]
            [StringLength(100, ErrorMessage = "A jelszónak legalább {2} karakter hosszúnak kell lennie.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Jelszó megerősítése")]
            [Required(ErrorMessage = "Erősítsd meg a jelszót.")]
            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "A két jelszó nem egyezik.")]
            public string ConfirmPassword { get; set; } = string.Empty;

            public string? ReturnUrl { get; set; }
        }
    }
}
