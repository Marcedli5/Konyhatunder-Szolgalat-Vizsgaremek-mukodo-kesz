using Backend_ASP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Backend_ASP.Controllers
{
    [ApiController]
    [Route("api")]
    public class PortalController : ControllerBase
    {
        private readonly LegacyMenuService _legacyMenuService;
        private readonly CartService _cartService;
        private readonly LegacyOrderService _legacyOrderService;
        private readonly UserManager<IdentityUser> _userManager;

        public PortalController(
            LegacyMenuService legacyMenuService,
            CartService cartService,
            LegacyOrderService legacyOrderService,
            UserManager<IdentityUser> userManager)
        {
            _legacyMenuService = legacyMenuService;
            _cartService = cartService;
            _legacyOrderService = legacyOrderService;
            _userManager = userManager;
        }

        [HttpGet("menu/upcoming")]
        public async Task<IActionResult> GetUpcomingMenuAsync([FromQuery] int days = 7, [FromQuery] DateOnly? from = null)
        {
            var menu = from.HasValue
                ? await _legacyMenuService.GetDailyMenusAsync(from.Value, from.Value.AddDays(days))
                : await _legacyMenuService.GetUpcomingDailyMenusAsync(days);

            return Ok(menu);
        }

        [Authorize]
        [HttpGet("cart")]
        public async Task<IActionResult> GetCartAsync()
        {
            return Ok(await _cartService.GetCartAsync());
        }

        [Authorize]
        [HttpPost("cart/items")]
        public async Task<IActionResult> AddCartItemAsync([FromBody] AddCartItemRequest request)
        {
            var result = await _cartService.AddItemAsync(request.MenuId, request.DeliveryDate, request.Quantity);
            if (!result.Success)
            {
                return BadRequest(new { message = result.ErrorMessage, cart = result.Cart });
            }

            return Ok(result.Cart);
        }

        [Authorize]
        [HttpPut("cart/items/{menuId}")]
        public async Task<IActionResult> UpdateCartItemAsync(ulong menuId, [FromBody] UpdateCartItemRequest request)
        {
            return Ok(await _cartService.UpdateQuantityAsync(menuId, request.Quantity));
        }

        [Authorize]
        [HttpDelete("cart/items/{menuId}")]
        public async Task<IActionResult> DeleteCartItemAsync(ulong menuId)
        {
            return Ok(await _cartService.RemoveItemAsync(menuId));
        }

        [Authorize]
        [HttpPost("orders/checkout")]
        public async Task<IActionResult> CheckoutAsync([FromBody] CheckoutRequest request)
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser is null)
            {
                return Unauthorized();
            }

            var cart = await _cartService.GetCartAsync();
            var result = await _legacyOrderService.CheckoutAsync(identityUser, cart, request.Comment);
            await _cartService.ClearAsync();
            return Ok(result);
        }

        [Authorize]
        [HttpGet("orders/me")]
        public async Task<IActionResult> GetMyOrdersAsync()
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser is null)
            {
                return Unauthorized();
            }

            return Ok(await _legacyOrderService.GetOrdersForUserAsync(identityUser));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("orders/all")]
        public async Task<IActionResult> GetAllOrdersAsync()
        {
            return Ok(await _legacyOrderService.GetAllOrdersAsync());
        }

        [Authorize]
        [HttpPost("tickets")]
        public async Task<IActionResult> CreateTicketAsync([FromBody] CreateTicketRequest request)
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser is null)
            {
                return Unauthorized();
            }

            await _legacyOrderService.CreateTicketAsync(identityUser, request.Description);
            return Ok(new { message = "A hibabejelentés rögzítve lett." });
        }
    }

    public record AddCartItemRequest(ulong MenuId, DateOnly DeliveryDate, int Quantity);

    public record UpdateCartItemRequest(int Quantity);

    public record CheckoutRequest(string? Comment);

    public record CreateTicketRequest(string Description);
}
