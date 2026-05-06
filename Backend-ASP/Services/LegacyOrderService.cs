using Konyhatunder_Szolgalat_Vizsgaremek.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Backend_ASP.Services
{
    public class LegacyOrderService
    {
        private readonly VizsgaremekEtlapContext _legacyContext;
        private readonly LegacyUserLinkService _legacyUserLinkService;

        public LegacyOrderService(VizsgaremekEtlapContext legacyContext, LegacyUserLinkService legacyUserLinkService)
        {
            _legacyContext = legacyContext;
            _legacyUserLinkService = legacyUserLinkService;
        }

        public async Task<OrderCheckoutResultDto> CheckoutAsync(IdentityUser identityUser, CartSummaryDto cart, string? comment)
        {
            if (cart.Items.Count == 0 || cart.DeliveryDate is null)
            {
                throw new InvalidOperationException("A kosár üres.");
            }

            var legacyUserId = await _legacyUserLinkService.EnsureLegacyUserAsync(identityUser, identityUser.UserName);

            var order = new Order
            {
                UserId = legacyUserId,
                OrderDate = cart.DeliveryDate.Value.ToDateTime(TimeOnly.MinValue),
                Status = "placed",
                Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim()
            };

            _legacyContext.Orders.Add(order);
            await _legacyContext.SaveChangesAsync();

            foreach (var cartItem in cart.Items)
            {
                _legacyContext.OrderItems.Add(new OrderItem
                {
                    OrderId = order.Id,
                    MenuId = cartItem.MenuId,
                    Qty = (uint)cartItem.Quantity,
                    UnitPriceFt = (uint)cartItem.UnitPriceFt
                });
            }

            await _legacyContext.SaveChangesAsync();

            return new OrderCheckoutResultDto(order.Id, cart.TotalPriceFt, cart.Items.Sum(item => item.Quantity), cart.DeliveryDate.Value);
        }

        public async Task<IReadOnlyList<UserOrderDto>> GetOrdersForUserAsync(IdentityUser identityUser)
        {
            var legacyUserId = await _legacyUserLinkService.EnsureLegacyUserAsync(identityUser, identityUser.UserName);
            return await QueryOrdersAsync(query => query.Where(order => order.UserId == legacyUserId));
        }

        public async Task<IReadOnlyList<UserOrderDto>> GetAllOrdersAsync()
        {
            return await QueryOrdersAsync(query => query);
        }

        public async Task CreateTicketAsync(IdentityUser identityUser, string description)
        {
            var trimmedDescription = description?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmedDescription))
            {
                throw new InvalidOperationException("A hibabejelentés szövege nem lehet üres.");
            }

            var legacyUserId = await _legacyUserLinkService.EnsureLegacyUserAsync(identityUser, identityUser.UserName);
            var ticketType = await GetIssueTicketTypeAsync();

            _legacyContext.Tickets.Add(new Ticket
            {
                UserId = legacyUserId,
                TicketTypeId = ticketType.Id,
                Description = trimmedDescription,
                Status = "open"
            });

            await _legacyContext.SaveChangesAsync();
        }

        private async Task<TicketType> GetIssueTicketTypeAsync()
        {
            var ticketType = await _legacyContext.TicketTypes
                .FirstOrDefaultAsync(item =>
                    item.Name == "Hibabejelentes" ||
                    item.Name == "Hibabejelentés" ||
                    item.Name == "Hibabejelentés" ||
                    item.Name == "WPF-issue");

            if (ticketType is not null)
            {
                return ticketType;
            }

            ticketType = new TicketType { Name = "Hibabejelentes" };
            _legacyContext.TicketTypes.Add(ticketType);
            await _legacyContext.SaveChangesAsync();
            return ticketType;
        }

        private async Task<IReadOnlyList<UserOrderDto>> QueryOrdersAsync(Func<IQueryable<Order>, IQueryable<Order>> queryBuilder)
        {
            var orders = await queryBuilder(_legacyContext.Orders.AsNoTracking())
                .Include(order => order.User)
                .Include(order => order.OrderItems)
                    .ThenInclude(orderItem => orderItem.Menu)
                .OrderByDescending(order => order.OrderDate)
                .ToListAsync();

            return orders.Select(order => new UserOrderDto(
                    order.Id,
                    order.User.FullName,
                    order.User.Email,
                    DateOnly.FromDateTime(order.OrderDate),
                    order.Status,
                    order.Comment,
                    order.OrderItems.Select(orderItem => new UserOrderItemDto(
                        orderItem.MenuId,
                        orderItem.Menu.Code,
                        orderItem.Qty,
                        orderItem.UnitPriceFt))
                    .ToList()))
                .ToList();
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
}
