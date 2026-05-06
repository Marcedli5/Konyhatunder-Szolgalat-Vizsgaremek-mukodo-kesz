using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Frontend_ASP.Services
{
    public class CartService
    {
        private const string CartSessionKey = "portal-cart";
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly LegacyMenuService _menuService;

        public CartService(IHttpContextAccessor httpContextAccessor, LegacyMenuService menuService)
        {
            _httpContextAccessor = httpContextAccessor;
            _menuService = menuService;
        }

        public async Task<CartSummaryDto> GetCartAsync()
        {
            return await LoadCartAsync();
        }

        public async Task<(bool Success, string? ErrorMessage, CartSummaryDto Cart)> AddItemAsync(ulong menuId, DateOnly deliveryDate, int quantity)
        {
            if (quantity <= 0)
            {
                return (false, "A mennyiségnek legalább 1-nek kell lennie.", await LoadCartAsync());
            }

            var menuOffer = await _menuService.GetMenuOfferAsync(menuId, deliveryDate);
            if (menuOffer is null)
            {
                return (false, "A kiválasztott menü már nem elérhető erre a napra.", await LoadCartAsync());
            }

            var cart = await LoadCartAsync();
            if (cart.Items.Count > 0 && cart.DeliveryDate != deliveryDate)
            {
                return (false, "Egy rendelésen belül csak azonos szállítási napra eső menüket lehet leadni.", cart);
            }

            var existingItem = cart.Items.FirstOrDefault(item => item.MenuId == menuId);
            if (existingItem is null)
            {
                cart.Items.Add(new CartItemDto(
                    menuOffer.MenuId,
                    menuOffer.MenuCode,
                    menuOffer.DisplayName,
                    quantity,
                    menuOffer.UnitPriceFt));
            }
            else
            {
                var updatedItem = existingItem with { Quantity = existingItem.Quantity + quantity };
                var index = cart.Items.IndexOf(existingItem);
                cart.Items[index] = updatedItem;
            }

            cart = cart with { DeliveryDate = deliveryDate };
            await SaveCartAsync(cart);
            return (true, null, cart with { TotalPriceFt = cart.Items.Sum(item => item.TotalPriceFt), TotalQuantity = cart.Items.Sum(item => item.Quantity) });
        }

        public async Task<CartSummaryDto> UpdateQuantityAsync(ulong menuId, int quantity)
        {
            var cart = await LoadCartAsync();
            var existingItem = cart.Items.FirstOrDefault(item => item.MenuId == menuId);
            if (existingItem is null)
            {
                return cart;
            }

            if (quantity <= 0)
            {
                cart.Items.Remove(existingItem);
            }
            else
            {
                var updatedItem = existingItem with { Quantity = quantity };
                var index = cart.Items.IndexOf(existingItem);
                cart.Items[index] = updatedItem;
            }

            if (cart.Items.Count == 0)
            {
                cart = cart with { DeliveryDate = null };
            }

            await SaveCartAsync(cart);
            return await LoadCartAsync();
        }

        public async Task<CartSummaryDto> RemoveItemAsync(ulong menuId)
        {
            return await UpdateQuantityAsync(menuId, 0);
        }

        public Task ClearAsync()
        {
            _httpContextAccessor.HttpContext?.Session.Remove(CartSessionKey);
            return Task.CompletedTask;
        }

        private async Task<CartSummaryDto> LoadCartAsync()
        {
            var rawCart = _httpContextAccessor.HttpContext?.Session.GetString(CartSessionKey);
            if (string.IsNullOrWhiteSpace(rawCart))
            {
                return EmptyCart();
            }

            var cart = JsonSerializer.Deserialize<CartSummaryDto>(rawCart) ?? EmptyCart();
            await Task.CompletedTask;
            return cart with
            {
                TotalPriceFt = cart.Items.Sum(item => item.TotalPriceFt),
                TotalQuantity = cart.Items.Sum(item => item.Quantity)
            };
        }

        private Task SaveCartAsync(CartSummaryDto cart)
        {
            var normalizedCart = cart with
            {
                TotalPriceFt = cart.Items.Sum(item => item.TotalPriceFt),
                TotalQuantity = cart.Items.Sum(item => item.Quantity)
            };

            var serializedCart = JsonSerializer.Serialize(normalizedCart);
            _httpContextAccessor.HttpContext?.Session.SetString(CartSessionKey, serializedCart);
            return Task.CompletedTask;
        }

        private static CartSummaryDto EmptyCart()
        {
            return new CartSummaryDto(null, [], 0, 0);
        }
    }

    public record CartSummaryDto(DateOnly? DeliveryDate, List<CartItemDto> Items, int TotalPriceFt, int TotalQuantity);

    public record CartItemDto(ulong MenuId, string MenuCode, string DisplayName, int Quantity, int UnitPriceFt)
    {
        public int TotalPriceFt => Quantity * UnitPriceFt;
    }
}
