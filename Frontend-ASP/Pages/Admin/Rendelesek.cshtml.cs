using Frontend_ASP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Frontend_ASP.Pages.Admin
{
    [Authorize(Policy = "AdminOnly")]
    public class RendelesekModel : PageModel
    {
        private readonly LegacyOrderService _legacyOrderService;

        public RendelesekModel(LegacyOrderService legacyOrderService)
        {
            _legacyOrderService = legacyOrderService;
        }

        public IReadOnlyList<UserOrderDto> Orders { get; private set; } = [];

        public async Task OnGetAsync()
        {
            Orders = await _legacyOrderService.GetAllOrdersAsync();
        }
    }
}
