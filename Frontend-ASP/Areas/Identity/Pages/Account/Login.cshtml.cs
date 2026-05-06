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
    public class LoginModel(ILogger<LoginModel> logger) : PageModel
    {
        private readonly ILogger<LoginModel> _logger = logger;

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? FormMessage { get; private set; }

        public bool OfferRegistration { get; private set; }

        public string RegistrationPrompt { get; private set; } = "A megadott e-mail címhez még nem tartozik fiók. Szeretnél regisztrálni?";

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
                new(ClaimTypes.Name, Input.Email.Split('@')[0]),
                new(ClaimTypes.Email, Input.Email.Trim())
            };

            if (Input.Email.StartsWith("admin", StringComparison.OrdinalIgnoreCase))
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = Input.RememberMe });

            _logger.LogInformation("Frontend cookie user logged in.");
            return LocalRedirect(returnUrl);
        }

        public class InputModel
        {
            [Display(Name = "E-mail cím")]
            [Required(ErrorMessage = "Add meg az e-mail címet.")]
            [EmailAddress(ErrorMessage = "Érvényes e-mail címet adj meg.")]
            public string Email { get; set; } = string.Empty;

            [Display(Name = "Jelszó")]
            [Required(ErrorMessage = "Add meg a jelszót.")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            public bool RememberMe { get; set; }

            public string? ReturnUrl { get; set; }
        }
    }
}
