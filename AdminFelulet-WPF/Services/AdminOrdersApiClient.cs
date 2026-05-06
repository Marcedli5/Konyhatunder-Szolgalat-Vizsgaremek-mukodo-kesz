using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace WPF_AdminFelulet.Services;

public sealed class AdminOrdersApiClient(HttpClient httpClient) : IAdminOrdersApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient = httpClient;

    public Task<IReadOnlyList<AdminOrderCustomerDto>> GetActiveUsersAsync(string? search = null)
    {
        var query = "active=true";
        if (!string.IsNullOrWhiteSpace(search))
        {
            query += $"&search={Uri.EscapeDataString(search.Trim())}";
        }

        return GetAsync<IReadOnlyList<AdminOrderCustomerDto>>($"api/admin/users?{query}");
    }

    public Task<IReadOnlyList<AdminMenuDto>> GetActiveMenusAsync()
        => GetAsync<IReadOnlyList<AdminMenuDto>>("api/admin/menus?active=true");

    public Task<AdminCreatedUserDto> CreateUserAsync(AdminCreateUserRequest request)
        => PostAsync<AdminCreateUserRequest, AdminCreatedUserDto>("api/admin/users", request);

    public Task<AdminCreateOrderResponse> CreateOrderAsync(AdminCreateOrderRequest request)
    {
        var firstItem = request.Items.FirstOrDefault()
            ?? throw new AdminApiException("Legalabb egy menu tetel szukseges a rendeles rogziteshez.");

        return PostAsync<AdminCreateOrderBackendRequest, AdminCreateOrderResponse>(
            "api/admin/orders",
            new AdminCreateOrderBackendRequest
            {
                CustomerId = request.UserId,
                MenuId = firstItem.MenuId,
                Quantity = firstItem.Quantity,
                DeliveryDate = request.DeliveryDate,
                Comment = request.Comment
            });
    }

    public Task<AdminTicketDto> CreateTicketAsync(AdminCreateTicketRequest request)
        => PostAsync<AdminCreateTicketRequest, AdminTicketDto>("api/admin/tickets", request);

    public Task<AdminFoodReferenceDataDto> GetFoodReferenceDataAsync()
        => GetAsync<AdminFoodReferenceDataDto>("api/admin/foods/reference-data");

    public Task<IReadOnlyList<AdminFoodListItemDto>> GetFoodsAsync(string? search = null, ulong? categoryId = null)
    {
        var queryParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(search))
        {
            queryParts.Add($"search={Uri.EscapeDataString(search.Trim())}");
        }

        if (categoryId.HasValue)
        {
            queryParts.Add($"categoryId={categoryId.Value}");
        }

        var query = queryParts.Count == 0 ? string.Empty : $"?{string.Join("&", queryParts)}";
        return GetAsync<IReadOnlyList<AdminFoodListItemDto>>($"api/admin/foods{query}");
    }

    public Task<AdminFoodListItemDto> CreateFoodAsync(AdminCreateFoodRequest request)
        => PostAsync<AdminCreateFoodRequest, AdminFoodListItemDto>("api/admin/foods", request);

    public Task<IReadOnlyList<AdminMenuWeekOptionDto>> GetMenuWeeksAsync()
        => GetAsync<IReadOnlyList<AdminMenuWeekOptionDto>>("api/admin/menu-weeks");

    public Task<AdminMenuEditorOptionsDto> GetMenuEditorOptionsAsync()
        => GetAsync<AdminMenuEditorOptionsDto>("api/admin/foods/menu-editor-options");

    public Task<AdminMenuWeekEditorDto> GetMenuWeekAsync(DateOnly weekStart)
        => GetAsync<AdminMenuWeekEditorDto>($"api/admin/menu-weeks/{weekStart:yyyy-MM-dd}");

    public Task SaveMenuWeekAsync(DateOnly weekStart, AdminSaveMenuWeekRequest request)
        => PutAsync($"api/admin/menu-weeks/{weekStart:yyyy-MM-dd}", request);

    public Task<IReadOnlyList<AdminOrderRowDto>> GetOrdersAsync(DateOnly dateFrom, DateOnly dateTo, string? search = null)
    {
        var query = $"from={dateFrom:yyyy-MM-dd}&to={dateTo:yyyy-MM-dd}";
        if (!string.IsNullOrWhiteSpace(search))
        {
            query += $"&search={Uri.EscapeDataString(search.Trim())}";
        }

        return GetAsync<IReadOnlyList<AdminOrderRowDto>>($"api/admin/orders?{query}");
    }

    public Task UpdateOrderItemAsync(ulong orderId, ulong menuId, AdminUpdateOrderItemRequest request)
        => PutAsync(
            $"api/admin/orders/{orderId}/items/{menuId}",
            new AdminUpdateOrderItemBackendRequest
            {
                CustomerName = request.Customer.FullName,
                Address = request.Customer.Address,
                Phone = request.Customer.Phone,
                Email = request.Customer.Email,
                MenuId = request.MenuId,
                Quantity = request.Quantity,
                DeliveryDate = request.DeliveryDate,
                Comment = request.Comment
            });

    public Task DeleteOrderItemAsync(ulong orderId, ulong menuId)
        => DeleteAsync($"api/admin/orders/{orderId}/items/{menuId}");

    private async Task<T> GetAsync<T>(string relativeUrl)
    {
        using var response = await _httpClient.GetAsync(relativeUrl);
        return await ReadResponseAsync<T>(response);
    }

    private async Task<TResponse> PostAsync<TRequest, TResponse>(string relativeUrl, TRequest request)
    {
        using var response = await _httpClient.PostAsJsonAsync(relativeUrl, request);
        return await ReadResponseAsync<TResponse>(response);
    }

    private async Task PutAsync<TRequest>(string relativeUrl, TRequest request)
    {
        using var response = await _httpClient.PutAsJsonAsync(relativeUrl, request);
        await EnsureSuccessAsync(response);
    }

    private async Task DeleteAsync(string relativeUrl)
    {
        using var response = await _httpClient.DeleteAsync(relativeUrl);
        await EnsureSuccessAsync(response);
    }

    private static async Task<T> ReadResponseAsync<T>(HttpResponseMessage response)
    {
        await EnsureSuccessAsync(response);

        var payload = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        if (payload is null)
        {
            throw new AdminApiException("Az API üres választ adott.", statusCode: (int)response.StatusCode);
        }

        return payload;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        ApiErrorResponse? apiError = null;
        try
        {
            apiError = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions);
        }
        catch
        {
            // Best-effort parse only.
        }

        var message = apiError?.Message;
        if (string.IsNullOrWhiteSpace(message))
        {
            message = response.StatusCode switch
            {
                HttpStatusCode.NotFound => "A kért API végpont vagy erőforrás nem található.",
                HttpStatusCode.BadRequest => "Az API kérés érvénytelen adatot kapott.",
                _ => $"Az API hívás sikertelen volt. HTTP {(int)response.StatusCode}."
            };
        }

        throw new AdminApiException(message, apiError?.Code, (int)response.StatusCode);
    }
}

public sealed class AdminApiException(string message, string? code = null, int? statusCode = null) : Exception(message)
{
    public string? Code { get; } = code;

    public int? StatusCode { get; } = statusCode;
}

public sealed class AdminOrderCustomerDto
{
    public ulong Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public bool? IsActive { get; set; }
}

public sealed class AdminMenuDto
{
    public ulong Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public int UnitPrice { get; set; }

    public bool? IsActive { get; set; }
}

public sealed class AdminCreatedUserDto
{
    public ulong Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Phone { get; set; }
}

public sealed class AdminCreateUserRequest
{
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string Address { get; set; } = string.Empty;
}

public sealed class AdminCreateOrderRequest
{
    public ulong UserId { get; set; }

    public DateOnly DeliveryDate { get; set; }

    public string? Comment { get; set; }

    public List<AdminCreateOrderItemRequest> Items { get; set; } = [];
}

internal sealed class AdminCreateOrderBackendRequest
{
    public ulong? CustomerId { get; set; }

    public ulong MenuId { get; set; }

    public int Quantity { get; set; }

    public DateOnly DeliveryDate { get; set; }

    public string? Comment { get; set; }
}

public sealed class AdminCreateOrderItemRequest
{
    public ulong MenuId { get; set; }

    public int Quantity { get; set; }

    public int UnitPrice { get; set; }
}

public sealed class AdminCreateOrderResponse
{
    public ulong OrderId { get; set; }

    public string Status { get; set; } = string.Empty;
}

public sealed class AdminCreateTicketRequest
{
    public ulong? UserId { get; set; }

    public AdminCreateUserRequest? Customer { get; set; }

    public string? TicketTypeName { get; set; }

    public string Description { get; set; } = string.Empty;
}

public sealed class AdminTicketDto
{
    public ulong Id { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string TicketType { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
}

public sealed class AdminFoodReferenceDataDto
{
    public IReadOnlyList<AdminLookupDto> Categories { get; set; } = [];

    public IReadOnlyList<AdminLookupDto> Allergens { get; set; } = [];
}

public sealed class AdminLookupDto
{
    public ulong Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public sealed class AdminFoodListItemDto
{
    public ulong Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public AdminLookupDto Category { get; set; } = new();

    public int? Calories { get; set; }

    public bool IsOnMenu { get; set; }

    public string Recipe { get; set; } = string.Empty;

    public IReadOnlyList<string> Ingredients { get; set; } = [];

    public IReadOnlyList<AdminLookupDto> Allergens { get; set; } = [];
}

public sealed class AdminCreateFoodRequest
{
    public string Name { get; set; } = string.Empty;

    public uint CategoryId { get; set; }

    public int? Calories { get; set; }

    public string? Recipe { get; set; }

    public List<string> Ingredients { get; set; } = [];

    public List<ulong> AllergenIds { get; set; } = [];
}

public sealed class AdminMenuWeekOptionDto
{
    public DateOnly WeekStart { get; set; }

    public DateOnly WeekEnd { get; set; }

    public string Label { get; set; } = string.Empty;

    public bool IsCurrentWeek { get; set; }

    public bool IsNextWeek { get; set; }
}

public sealed class AdminMenuEditorOptionsDto
{
    public IReadOnlyList<AdminFoodOptionDto> Starters { get; set; } = [];

    public IReadOnlyList<AdminFoodOptionDto> MainCourses { get; set; } = [];

    public IReadOnlyList<AdminFoodOptionDto> SideDishesAndSalads { get; set; } = [];

    public IReadOnlyList<AdminFoodOptionDto> PlusDesserts { get; set; } = [];
}

public sealed class AdminFoodOptionDto
{
    public ulong Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public sealed class AdminMenuWeekEditorDto
{
    public DateOnly WeekStart { get; set; }

    public IReadOnlyList<AdminMenuEditorDayDto> Days { get; set; } = [];
}

public sealed class AdminMenuEditorDayDto
{
    public DateOnly Date { get; set; }

    public string DayName { get; set; } = string.Empty;

    public IReadOnlyList<AdminMenuEditorMenuDto> Menus { get; set; } = [];

    public AdminPlusMenuDto? Plus { get; set; }
}

public sealed class AdminMenuEditorMenuDto
{
    public string Code { get; set; } = string.Empty;

    public ulong StarterFoodId { get; set; }

    public ulong MainCourseFoodId { get; set; }

    public ulong SideDishFoodId { get; set; }
}

public sealed class AdminPlusMenuDto
{
    public ulong FoodId { get; set; }
}

public sealed class AdminSaveMenuWeekRequest
{
    public IReadOnlyList<AdminSaveMenuDayRequest> Days { get; set; } = [];
}

public sealed class AdminSaveMenuDayRequest
{
    public DateOnly Date { get; set; }

    public IReadOnlyList<AdminMenuEditorMenuDto> Menus { get; set; } = [];

    public AdminPlusMenuDto? Plus { get; set; }
}

public sealed class AdminOrderRowDto
{
    public ulong OrderId { get; set; }

    public ulong MenuId { get; set; }

    public ulong UserId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public DateOnly DeliveryDate { get; set; }

    public string MenuCode { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public int UnitPrice { get; set; }

    public string? Comment { get; set; }
}

public sealed class AdminUpdateOrderItemRequest
{
    public AdminOrderCustomerUpdateDto Customer { get; set; } = new();

    public DateOnly DeliveryDate { get; set; }

    public string? Comment { get; set; }

    public ulong MenuId { get; set; }

    public int Quantity { get; set; }

    public int UnitPrice { get; set; }
}

public sealed class AdminOrderCustomerUpdateDto
{
    public string FullName { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string Email { get; set; } = string.Empty;
}

internal sealed class AdminUpdateOrderItemBackendRequest
{
    public string CustomerName { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string Email { get; set; } = string.Empty;

    public ulong MenuId { get; set; }

    public int Quantity { get; set; }

    public DateOnly DeliveryDate { get; set; }

    public string? Comment { get; set; }
}

public sealed class ApiErrorResponse
{
    public string? Code { get; set; }

    public string? Message { get; set; }
}
