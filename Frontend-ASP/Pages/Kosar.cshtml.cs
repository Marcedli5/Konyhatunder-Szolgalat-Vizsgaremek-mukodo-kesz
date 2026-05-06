using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Frontend_ASP.Pages
{
    [Authorize]
    public class KosarModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
