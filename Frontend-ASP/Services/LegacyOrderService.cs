using System.Security.Claims;

namespace Frontend_ASP.Services
{
    public class LegacyOrderService(BackendApiClient backendApiClient)
    {
        private readonly BackendApiClient _backendApiClient = backendApiClient;

        public async Task<OrderCheckoutResultDto> CheckoutAsync(ClaimsPrincipal user, CartSummaryDto cart, string? comment)
        {
            if (cart.Items.Count == 0 || cart.DeliveryDate is null)
            {
                throw new InvalidOperationException("A kosar ures.");
            }

            var customer = new CreateCustomerRequest(
                user.Identity?.Name ?? "Webes megrendelo",
                user.FindFirstValue(ClaimTypes.Email) ?? $"{Guid.NewGuid():N}@frontend.local",
                user.FindFirstValue(ClaimTypes.MobilePhone),
                user.FindFirstValue(ClaimTypes.StreetAddress));

            var createdOrder = await _backendApiClient.PostAsync<CreateAdminOrderRequest, OrderListItemDto>(
                "api/admin/orders",
                new CreateAdminOrderRequest(
                    null,
                    customer,
                    null,
                    null,
                    cart.DeliveryDate.Value,
                    comment,
                    cart.Items.Select(item => new CreateAdminOrderItemRequest(item.MenuId, item.Quantity)).ToList()));

            if (createdOrder is null)
            {
                throw new InvalidOperationException("A Backend nem adott valaszt a rendeles rogzitesere.");
            }

            return new OrderCheckoutResultDto(
                createdOrder.OrderId,
                cart.TotalPriceFt,
                cart.TotalQuantity,
                cart.DeliveryDate.Value);
        }

        public async Task<IReadOnlyList<UserOrderDto>> GetOrdersForUserAsync(ClaimsPrincipal user)
        {
            var email = user.FindFirstValue(ClaimTypes.Email);
            var orders = await GetAllOrdersAsync();
            return string.IsNullOrWhiteSpace(email)
                ? orders
                : orders.Where(order => string.Equals(order.CustomerEmail, email, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public async Task<IReadOnlyList<UserOrderDto>> GetAllOrdersAsync()
        {
            var orderItems = await _backendApiClient.GetAsync<IReadOnlyList<OrderListItemDto>>("api/admin/orders") ?? [];
            return orderItems
                .GroupBy(item => item.OrderId)
                .Select(group =>
                {
                    var first = group.First();
                    return new UserOrderDto(
                        first.OrderId,
                        first.CustomerName,
                        first.Email,
                        first.DeliveryDate,
                        "placed",
                        first.Comment,
                        group.Select(item => new UserOrderItemDto(
                            item.MenuId,
                            item.MenuCode,
                            (uint)item.Quantity,
                            (uint)item.UnitPrice))
                        .ToList());
                })
                .OrderByDescending(order => order.DeliveryDate)
                .ToList();
        }

        public async Task CreateTicketAsync(ClaimsPrincipal user, string description)
        {
            var trimmedDescription = description?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmedDescription))
            {
                throw new InvalidOperationException("A hibabejelentes szovege nem lehet ures.");
            }

            await _backendApiClient.PostAsync(
                "api/admin/tickets",
                new CreateAdminTicketRequest(
                    null,
                    new CreateCustomerRequest(
                        user.Identity?.Name ?? "Webes felhasznalo",
                        user.FindFirstValue(ClaimTypes.Email) ?? $"{Guid.NewGuid():N}@frontend.local",
                        null,
                        null),
                    "Hibabejelentes",
                    trimmedDescription));
        }
    }

    public record OrderCheckoutResultDto(ulong OrderId, int TotalPriceFt, int TotalQuantity, DateOnly DeliveryDate);

    public record UserOrderDto(
        ulong OrderId,
        string CustomerName,
        string CustomerEmail,
        DateOnly DeliveryDate,
        string Status,
        string? Comment,
        IReadOnlyList<UserOrderItemDto> Items)
    {
        public int TotalPriceFt => Items.Sum(item => item.TotalPriceFt);
    }

    public record UserOrderItemDto(ulong MenuId, string MenuCode, uint Quantity, uint UnitPriceFt)
    {
        public int TotalPriceFt => (int)(Quantity * UnitPriceFt);
    }

    public sealed record CreateCustomerRequest(string FullName, string Email, string? Phone, string? Address);

    public sealed record CreateAdminOrderRequest(
        ulong? CustomerId,
        CreateCustomerRequest? NewCustomer,
        ulong? MenuId,
        int? Quantity,
        DateOnly DeliveryDate,
        string? Comment,
        IReadOnlyList<CreateAdminOrderItemRequest>? Items = null);

    public sealed record CreateAdminOrderItemRequest(ulong MenuId, int Quantity);

    public sealed record OrderListItemDto(
        ulong OrderId,
        ulong MenuId,
        ulong CustomerId,
        string CustomerName,
        string? Address,
        string? Phone,
        string Email,
        DateOnly DeliveryDate,
        string MenuCode,
        int Quantity,
        int UnitPrice,
        string? Comment);

    public sealed record CreateAdminTicketRequest(
        ulong? UserId,
        CreateCustomerRequest? Customer,
        string? TicketTypeName,
        string Description);
}
