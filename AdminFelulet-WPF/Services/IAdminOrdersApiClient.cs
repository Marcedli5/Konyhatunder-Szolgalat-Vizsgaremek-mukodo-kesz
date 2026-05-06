namespace WPF_AdminFelulet.Services;

public interface IAdminOrdersApiClient
{
    Task<IReadOnlyList<AdminOrderCustomerDto>> GetActiveUsersAsync(string? search = null);

    Task<IReadOnlyList<AdminMenuDto>> GetActiveMenusAsync();

    Task<AdminCreatedUserDto> CreateUserAsync(AdminCreateUserRequest request);

    Task<AdminCreateOrderResponse> CreateOrderAsync(AdminCreateOrderRequest request);

    Task<AdminTicketDto> CreateTicketAsync(AdminCreateTicketRequest request);

    Task<AdminFoodReferenceDataDto> GetFoodReferenceDataAsync();

    Task<IReadOnlyList<AdminFoodListItemDto>> GetFoodsAsync(string? search = null, ulong? categoryId = null);

    Task<AdminFoodListItemDto> CreateFoodAsync(AdminCreateFoodRequest request);

    Task<IReadOnlyList<AdminMenuWeekOptionDto>> GetMenuWeeksAsync();

    Task<AdminMenuEditorOptionsDto> GetMenuEditorOptionsAsync();

    Task<AdminMenuWeekEditorDto> GetMenuWeekAsync(DateOnly weekStart);

    Task SaveMenuWeekAsync(DateOnly weekStart, AdminSaveMenuWeekRequest request);

    Task<IReadOnlyList<AdminOrderRowDto>> GetOrdersAsync(DateOnly dateFrom, DateOnly dateTo, string? search = null);

    Task UpdateOrderItemAsync(ulong orderId, ulong menuId, AdminUpdateOrderItemRequest request);

    Task DeleteOrderItemAsync(ulong orderId, ulong menuId);
}
