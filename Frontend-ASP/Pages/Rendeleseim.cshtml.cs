using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Frontend_ASP.Services;

namespace Frontend_ASP.Pages
{
    [Authorize]
    public class RendeleseimModel : PageModel
    {
        private readonly LegacyOrderService _legacyOrderService;

        public RendeleseimModel(LegacyOrderService legacyOrderService)
        {
            _legacyOrderService = legacyOrderService;
        }

        public IReadOnlyList<UserOrderDto> Orders { get; private set; } = [];

        public async Task OnGetAsync()
        {
            Orders = await _legacyOrderService.GetOrdersForUserAsync(User);
        }
    }
}
