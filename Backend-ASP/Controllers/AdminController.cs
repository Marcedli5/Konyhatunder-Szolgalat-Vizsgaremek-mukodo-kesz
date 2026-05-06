using System.Globalization;
using System.Text.Json;
using Konyhatunder_Szolgalat_Vizsgaremek.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LegacyMenuItem = Konyhatunder_Szolgalat_Vizsgaremek.Models.MenuItem;

namespace Backend_ASP.Controllers;

[ApiController]
[Route("api/admin")]
public sealed class AdminController : ControllerBase
{
    private const string MenuEditorNotePrefix = "[WPF_MENU_EDITOR]";
    private static readonly CultureInfo HungarianCulture = new("hu-HU");
    private readonly VizsgaremekEtlapContext _context;

    public AdminController(VizsgaremekEtlapContext context)
    {
        _context = context;
    }

    [HttpGet("menu-weeks")]
    public ActionResult<IReadOnlyList<MenuWeekOptionDto>> GetMenuWeeks([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var currentWeekStart = GetWeekStart(DateOnly.FromDateTime(DateTime.Today));
        var firstWeekStart = GetWeekStart(from ?? currentWeekStart.AddDays(-21));
        var lastWeekStart = GetWeekStart(to ?? currentWeekStart.AddDays(7));
        var weeks = new List<MenuWeekOptionDto>();

        for (var weekStart = firstWeekStart; weekStart <= lastWeekStart; weekStart = weekStart.AddDays(7))
        {
            var weekEnd = weekStart.AddDays(5);
            weeks.Add(new MenuWeekOptionDto(
                weekStart,
                weekEnd,
                $"{weekStart:yyyy.MM.dd.} - {weekEnd:MM.dd.}",
                weekStart == currentWeekStart,
                weekStart == currentWeekStart.AddDays(7)));
        }

        return Ok(weeks);
    }

    [HttpGet("foods/menu-editor-options")]
    public async Task<ActionResult<MenuEditorOptionsDto>> GetMenuEditorOptionsAsync()
    {
        var foods = await _context.Foods
            .AsNoTracking()
            .Include(food => food.Category)
            .OrderBy(food => food.Name)
            .ToListAsync();

        return Ok(new MenuEditorOptionsDto(
            ToFoodOptions(foods.Where(food => IsCategory(food.Category.Name, "leves"))),
            ToFoodOptions(foods.Where(food => IsCategory(food.Category.Name, "foetel") || IsCategory(food.Category.Name, "desszert"))),
            ToFoodOptions(foods.Where(food => IsCategory(food.Category.Name, "koret") || IsCategory(food.Category.Name, "salata"))),
            ToFoodOptions(foods.Where(food => IsCategory(food.Category.Name, "desszert")))));
    }

    [HttpGet("menu-weeks/{weekStart}")]
    public async Task<ActionResult<MenuWeekEditorDto>> GetMenuWeekAsync(DateOnly weekStart)
    {
        weekStart = GetWeekStart(weekStart);

        var menuDefinitions = await _context.Menus
            .AsNoTracking()
            .Include(menu => menu.MenuItems)
            .Where(menu => menu.IsActive != false)
            .ToDictionaryAsync(menu => menu.Code, menu => menu);

        var dailyMenus = await _context.DailyMenus
            .AsNoTracking()
            .Include(day => day.MenuAvailabilities)
                .ThenInclude(availability => availability.Menu)
            .Where(day => day.MenuDate >= weekStart && day.MenuDate <= weekStart.AddDays(5))
            .ToDictionaryAsync(day => day.MenuDate, day => day);

        var days = Enumerable.Range(0, 6)
            .Select(offset =>
            {
                var date = weekStart.AddDays(offset);
                dailyMenus.TryGetValue(date, out var dailyMenu);
                var availableCodes = dailyMenu?.MenuAvailabilities.Select(item => item.Menu.Code).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
                var editorState = ReadMenuEditorState(dailyMenu?.Note);

                return new MenuEditorDayDto(
                    date,
                    ToHungarianDayName(date),
                    new[] { "A", "B", "C" }
                        .Select(code => availableCodes.Contains(code)
                            ? ToMenuEditorMenuDto(
                                code,
                                editorState,
                                menuDefinitions.GetValueOrDefault(code))
                            : new MenuEditorMenuDto(code, 0, 0, 0))
                        .ToList(),
                    availableCodes.Contains("P") || availableCodes.Contains("PLUS")
                        ? ToPlusMenuDto(
                            editorState,
                            menuDefinitions.GetValueOrDefault("P"))
                        : null);
            })
            .ToList();

        return Ok(new MenuWeekEditorDto(weekStart, days));
    }

    [HttpPut("menu-weeks/{weekStart}")]
    public async Task<IActionResult> SaveMenuWeekAsync(DateOnly weekStart, [FromBody] SaveMenuWeekRequest request)
    {
        weekStart = GetWeekStart(weekStart);
        var menus = await _context.Menus
            .Include(menu => menu.MenuItems)
            .Where(menu => menu.IsActive != false)
            .ToDictionaryAsync(menu => menu.Code);

        foreach (var day in request.Days)
        {
            var dailyMenu = await GetOrCreateDailyMenuAsync(day.Date);
            var requestedMenus = day.Menus.ToDictionary(item => item.Code, StringComparer.OrdinalIgnoreCase);
            dailyMenu.Note = WriteMenuEditorState(day);

            foreach (var code in new[] { "A", "B", "C" })
            {
                if (!menus.TryGetValue(code, out var menu))
                {
                    continue;
                }

                if (requestedMenus.ContainsKey(code))
                {
                    await EnsureAvailabilityAsync(dailyMenu.Id, menu.Id);
                }
                else
                {
                    var existingAvailability = await _context.MenuAvailabilities.FindAsync(dailyMenu.Id, menu.Id);
                    if (existingAvailability is not null)
                    {
                        _context.MenuAvailabilities.Remove(existingAvailability);
                    }
                }
            }

            if (day.Plus is not null && menus.TryGetValue("P", out var plusMenu))
            {
                await EnsureAvailabilityAsync(dailyMenu.Id, plusMenu.Id);
            }
            else if (menus.TryGetValue("P", out var removedPlusMenu))
            {
                var existingPlus = await _context.MenuAvailabilities.FindAsync(dailyMenu.Id, removedPlusMenu.Id);
                if (existingPlus is not null)
                {
                    _context.MenuAvailabilities.Remove(existingPlus);
                }
            }
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("foods/reference-data")]
    public async Task<ActionResult<FoodReferenceDataDto>> GetFoodReferenceDataAsync()
    {
        var categories = await _context.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .Select(category => new LookupDto(category.Id, category.Name))
            .ToListAsync();

        var allergens = await _context.Allergens
            .AsNoTracking()
            .OrderBy(allergen => allergen.Name)
            .Select(allergen => new LookupDto(allergen.Id, allergen.Name))
            .ToListAsync();

        return Ok(new FoodReferenceDataDto(categories, allergens));
    }

    [HttpGet("foods")]
    public async Task<ActionResult<IReadOnlyList<FoodListItemDto>>> GetFoodsAsync([FromQuery] string? search, [FromQuery] uint? categoryId)
    {
        var query = _context.Foods
            .AsNoTracking()
            .Include(food => food.Category)
            .Include(food => food.Allergens)
            .Include(food => food.RecipeItems)
                .ThenInclude(recipe => recipe.Ingredient)
            .Include(food => food.MenuItems)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(food => food.Name.Contains(search));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(food => food.CategoryId == categoryId.Value);
        }

        var foods = await query.OrderBy(food => food.Name).ToListAsync();
        return Ok(foods.Select(ToFoodListItemDto).ToList());
    }

    [HttpPost("foods")]
    public async Task<ActionResult<FoodListItemDto>> CreateFoodAsync([FromBody] CreateFoodRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { code = "missing_food_name", message = "Az étel neve kötelező." });
        }

        var ingredientLines = (request.Ingredients ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (ingredientLines.Count == 0)
        {
            return BadRequest(new { code = "missing_ingredients", message = "Legalább egy hozzávalót meg kell adni." });
        }

        if (!await _context.Categories.AnyAsync(category => category.Id == request.CategoryId))
        {
            return BadRequest(new { code = "unknown_category", message = "A kiválasztott kategória nem található." });
        }

        var food = new Food
        {
            Name = request.Name.Trim(),
            CategoryId = request.CategoryId,
            Kcal = request.Calories,
        };

        foreach (var allergenId in (request.AllergenIds ?? []).Distinct())
        {
            if (allergenId > ushort.MaxValue)
            {
                continue;
            }

            var allergen = await _context.Allergens.FindAsync((ushort)allergenId);
            if (allergen is not null)
            {
                food.Allergens.Add(allergen);
            }
        }

        _context.Foods.Add(food);

        foreach (var ingredientLine in ingredientLines)
        {
            var ingredient = await GetOrCreateIngredientAsync(ingredientLine);
            _context.RecipeItems.Add(new RecipeItem
            {
                Food = food,
                Ingredient = ingredient,
                Amount = 1,
                Unit = "db"
            });
        }

        await _context.SaveChangesAsync();

        var created = await _context.Foods
            .AsNoTracking()
            .Include(item => item.Category)
            .Include(item => item.Allergens)
            .Include(item => item.RecipeItems)
                .ThenInclude(item => item.Ingredient)
            .Include(item => item.MenuItems)
            .FirstAsync(item => item.Id == food.Id);

        return Ok(ToFoodListItemDto(created));
    }

    [HttpGet("orders/create-options")]
    public async Task<ActionResult<OrderCreateOptionsDto>> GetOrderCreateOptionsAsync()
    {
        var customers = await _context.Users
            .AsNoTracking()
            .Where(user => user.IsActive != false)
            .OrderBy(user => user.FullName)
            .Select(user => new OrderCustomerDto(user.Id, user.FullName, user.Address, user.Email, user.Phone))
            .ToListAsync();

        var menus = await GetOrderMenuOptionsAsync();
        return Ok(new OrderCreateOptionsDto(customers, menus));
    }

    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<AdminUserDto>>> GetUsersAsync([FromQuery] bool? active, [FromQuery] string? search)
    {
        var query = _context.Users.AsNoTracking().AsQueryable();

        if (active.HasValue)
        {
            query = query.Where(user => (user.IsActive != false) == active.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(user => user.FullName.Contains(search) || (user.Address != null && user.Address.Contains(search)));
        }

        var users = await query
            .OrderBy(user => user.FullName)
            .Select(user => new AdminUserDto(user.Id, user.FullName, user.Address, user.Email, user.Phone, user.IsActive))
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost("users")]
    public async Task<ActionResult<AdminCreatedUserDto>> CreateUserAsync([FromBody] CreateCustomerRequest request)
    {
        var userId = await CreateCustomerAsync(request);
        var user = await _context.Users.AsNoTracking().FirstAsync(item => item.Id == userId);
        return Ok(new AdminCreatedUserDto(user.Id, user.FullName, user.Address, user.Email, user.Phone));
    }

    [HttpGet("menus")]
    public async Task<ActionResult<IReadOnlyList<AdminMenuDto>>> GetMenusAsync([FromQuery] bool? active)
    {
        var query = _context.Menus
            .AsNoTracking()
            .Where(menu => menu.Code == "A" || menu.Code == "B" || menu.Code == "C" || menu.Code == "P")
            .AsQueryable();

        if (active.HasValue)
        {
            query = query.Where(menu => (menu.IsActive != false) == active.Value);
        }

        var menus = await query
            .OrderBy(menu => menu.Code)
            .Select(menu => new AdminMenuDto(menu.Id, menu.Code, (int)menu.PriceFt, menu.IsActive))
            .ToListAsync();

        return Ok(menus);
    }

    [HttpPost("orders")]
    public async Task<ActionResult<OrderListItemDto>> CreateOrderAsync([FromBody] CreateAdminOrderRequest request)
    {
        var customerId = request.CustomerId ?? await CreateCustomerAsync(request.NewCustomer);
        var requestedItems = request.Items is { Count: > 0 }
            ? request.Items
            : request.MenuId.HasValue && request.Quantity.HasValue
                ? [new CreateAdminOrderItemRequest(request.MenuId.Value, request.Quantity.Value)]
                : [];

        if (requestedItems.Count == 0)
        {
            return BadRequest(new { code = "empty_order", message = "Legalább egy menü tétel szükséges a rendeléshez." });
        }

        if (requestedItems.Any(item => item.Quantity <= 0))
        {
            return BadRequest(new { code = "invalid_quantity", message = "A mennyiségnek legalább 1-nek kell lennie." });
        }

        var menuIds = requestedItems.Select(item => item.MenuId).Distinct().ToList();
        if (menuIds.Count != requestedItems.Count)
        {
            return BadRequest(new { code = "duplicate_menu_item", message = "Egy rendelésen belül egy menü csak egyszer szerepelhet." });
        }

        var menus = await _context.Menus
            .Where(menu => menuIds.Contains(menu.Id))
            .ToDictionaryAsync(menu => menu.Id);

        if (menus.Count != menuIds.Count)
        {
            return BadRequest(new { code = "unknown_menu", message = "A rendelés ismeretlen menüt tartalmaz." });
        }

        var order = new Order
        {
            UserId = customerId,
            OrderDate = request.DeliveryDate.ToDateTime(TimeOnly.MinValue),
            Status = "placed",
            Comment = NormalizeOptional(request.Comment)
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        foreach (var item in requestedItems)
        {
            var menu = menus[item.MenuId];
            _context.OrderItems.Add(new OrderItem
            {
                OrderId = order.Id,
                MenuId = menu.Id,
                Qty = (uint)item.Quantity,
                UnitPriceFt = menu.PriceFt
            });
        }

        await _context.SaveChangesAsync();
        return Ok((await QueryOrderItemsAsync(order.Id, null)).First());
    }

    [HttpGet("orders")]
    public async Task<ActionResult<IReadOnlyList<OrderListItemDto>>> GetOrdersAsync([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] string? search)
    {
        var items = await QueryOrderItemsAsync(null, null, from, to, search);
        return Ok(items);
    }

    [HttpPut("orders/{orderId}/items/{menuId}")]
    public async Task<IActionResult> UpdateOrderItemAsync(ulong orderId, ulong menuId, [FromBody] UpdateAdminOrderRequest request)
    {
        var orderItem = await _context.OrderItems
            .Include(item => item.Order)
                .ThenInclude(order => order.User)
            .FirstOrDefaultAsync(item => item.OrderId == orderId && item.MenuId == menuId);

        if (orderItem is null)
        {
            return NotFound();
        }

        orderItem.Order.OrderDate = request.DeliveryDate.ToDateTime(TimeOnly.MinValue);
        orderItem.Order.Comment = NormalizeOptional(request.Comment);
        orderItem.Order.User.FullName = request.CustomerName.Trim();
        orderItem.Order.User.Address = NormalizeOptional(request.Address);
        orderItem.Order.User.Phone = NormalizeOptional(request.Phone);
        orderItem.Order.User.Email = request.Email.Trim();

        if (request.MenuId != menuId)
        {
            if (await _context.OrderItems.AnyAsync(item => item.OrderId == orderId && item.MenuId == request.MenuId))
            {
                return Conflict(new { code = "duplicate_menu_item", message = "Ehhez a rendeleshez mar tartozik ilyen menu." });
            }

            var menu = await _context.Menus.FirstAsync(item => item.Id == request.MenuId);
            _context.OrderItems.Remove(orderItem);
            _context.OrderItems.Add(new OrderItem
            {
                OrderId = orderId,
                MenuId = menu.Id,
                Qty = (uint)request.Quantity,
                UnitPriceFt = menu.PriceFt
            });
        }
        else
        {
            var menu = await _context.Menus.FirstAsync(item => item.Id == menuId);
            orderItem.Qty = (uint)request.Quantity;
            orderItem.UnitPriceFt = menu.PriceFt;
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("orders/{orderId}/items/{menuId}")]
    public async Task<IActionResult> DeleteOrderItemAsync(ulong orderId, ulong menuId)
    {
        var orderItem = await _context.OrderItems.FirstOrDefaultAsync(item => item.OrderId == orderId && item.MenuId == menuId);
        if (orderItem is null)
        {
            return NotFound();
        }

        _context.OrderItems.Remove(orderItem);
        var hasOtherItems = await _context.OrderItems.AnyAsync(item => item.OrderId == orderId && item.MenuId != menuId);
        if (!hasOtherItems)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order is not null)
            {
                _context.Orders.Remove(order);
            }
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("tickets")]
    public async Task<ActionResult<IReadOnlyList<TicketDto>>> GetTicketsAsync()
    {
        var tickets = await _context.Tickets
            .AsNoTracking()
            .Include(ticket => ticket.User)
            .Include(ticket => ticket.TicketType)
            .OrderByDescending(ticket => ticket.Id)
            .Select(ticket => new TicketDto(
                ticket.Id,
                ticket.User.FullName,
                ticket.TicketType.Name,
                ticket.Description,
                ticket.Status))
            .ToListAsync();

        return Ok(tickets);
    }

    [HttpPost("tickets")]
    public async Task<ActionResult<TicketDto>> CreateTicketAsync([FromBody] CreateAdminTicketRequest request)
    {
        var userId = request.UserId ?? await CreateCustomerAsync(request.Customer);
        var ticketType = await GetOrCreateTicketTypeAsync(request.TicketTypeName ?? "WPF-issue");
        var ticket = new Ticket
        {
            UserId = userId,
            TicketTypeId = ticketType.Id,
            Description = request.Description.Trim(),
            Status = "open"
        };

        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();

        var user = await _context.Users.FindAsync(userId);
        return Ok(new TicketDto(ticket.Id, user?.FullName ?? "Ismeretlen", ticketType.Name, ticket.Description, ticket.Status));
    }

    private async Task<DailyMenu> GetOrCreateDailyMenuAsync(DateOnly date)
    {
        var dailyMenu = await _context.DailyMenus.FirstOrDefaultAsync(item => item.MenuDate == date);
        if (dailyMenu is not null)
        {
            return dailyMenu;
        }

        dailyMenu = new DailyMenu { MenuDate = date, Note = "Admin API altal rogzitett napi kinalat." };
        _context.DailyMenus.Add(dailyMenu);
        await _context.SaveChangesAsync();
        return dailyMenu;
    }

    private async Task<MenuAvailability> EnsureAvailabilityAsync(ulong dailyMenuId, ulong menuId)
    {
        var availability = await _context.MenuAvailabilities.FindAsync(dailyMenuId, menuId);
        if (availability is not null)
        {
            return availability;
        }

        availability = new MenuAvailability { DailyMenuId = dailyMenuId, MenuId = menuId, MaxQty = 50 };
        _context.MenuAvailabilities.Add(availability);
        return availability;
    }

    private static void UpdateMenuItems(Menu menu, IReadOnlyList<ulong> foodIds)
    {
        menu.MenuItems.Clear();
        for (var index = 0; index < foodIds.Count; index++)
        {
            menu.MenuItems.Add(new LegacyMenuItem { MenuId = menu.Id, FoodId = foodIds[index], CourseOrder = (byte)(index + 1) });
        }
    }

    private async Task<Ingredient> GetOrCreateIngredientAsync(string name)
    {
        var ingredient = await _context.Ingredients.FirstOrDefaultAsync(item => item.Name == name);
        if (ingredient is not null)
        {
            return ingredient;
        }

        ingredient = new Ingredient { Name = name, BaseUnit = "db" };
        _context.Ingredients.Add(ingredient);
        return ingredient;
    }

    private async Task<IReadOnlyList<OrderMenuDto>> GetOrderMenuOptionsAsync()
    {
        return await _context.Menus
            .AsNoTracking()
            .Where(menu => menu.IsActive != false)
            .OrderBy(menu => menu.Code)
            .Select(menu => new OrderMenuDto(menu.Id, menu.Code, (int)menu.PriceFt))
            .ToListAsync();
    }

    private async Task<IReadOnlyList<OrderListItemDto>> QueryOrderItemsAsync(
        ulong? orderId,
        ulong? menuId,
        DateOnly? from = null,
        DateOnly? to = null,
        string? search = null)
    {
        var query = _context.OrderItems
            .AsNoTracking()
            .Include(item => item.Order)
                .ThenInclude(order => order.User)
            .Include(item => item.Menu)
            .AsQueryable();

        if (orderId.HasValue)
        {
            query = query.Where(item => item.OrderId == orderId.Value);
        }

        if (menuId.HasValue)
        {
            query = query.Where(item => item.MenuId == menuId.Value);
        }

        if (from.HasValue)
        {
            query = query.Where(item => item.Order.OrderDate >= from.Value.ToDateTime(TimeOnly.MinValue));
        }

        if (to.HasValue)
        {
            query = query.Where(item => item.Order.OrderDate < to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(item => item.Order.User.FullName.Contains(search) || (item.Order.User.Address != null && item.Order.User.Address.Contains(search)));
        }

        var items = await query
            .OrderBy(item => item.Order.OrderDate)
            .ThenBy(item => item.Order.User.FullName)
            .ThenBy(item => item.Menu.Code)
            .ToListAsync();

        return items.Select(item => new OrderListItemDto(
            item.OrderId,
            item.MenuId,
            item.Order.UserId,
            item.Order.User.FullName,
            item.Order.User.Address,
            item.Order.User.Phone,
            item.Order.User.Email,
            DateOnly.FromDateTime(item.Order.OrderDate),
            item.Menu.Code,
            (int)item.Qty,
            (int)item.UnitPriceFt,
            item.Order.Comment))
            .ToList();
    }

    private async Task<ulong> CreateCustomerAsync(CreateCustomerRequest? request)
    {
        if (request is null)
        {
            throw new InvalidOperationException("Uj ugyfel adatai hianyoznak.");
        }

        var user = await _context.Users.FirstOrDefaultAsync(item => item.Email == request.Email.Trim());
        if (user is not null)
        {
            user.FullName = request.FullName.Trim();
            user.Phone = NormalizeOptional(request.Phone);
            user.Address = NormalizeOptional(request.Address);
            user.IsActive = true;
            await _context.SaveChangesAsync();
            return user.Id;
        }

        user = new User
        {
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim(),
            Phone = NormalizeOptional(request.Phone),
            Address = NormalizeOptional(request.Address),
            PasswordHash = "admin-created-account",
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user.Id;
    }

    private async Task<TicketType> GetOrCreateTicketTypeAsync(string name)
    {
        name = name.Trim();
        var ticketType = await _context.TicketTypes.FirstOrDefaultAsync(item => item.Name == name);
        if (ticketType is not null)
        {
            return ticketType;
        }

        ticketType = new TicketType { Name = name };
        _context.TicketTypes.Add(ticketType);
        await _context.SaveChangesAsync();
        return ticketType;
    }

    private static IReadOnlyList<FoodOptionDto> ToFoodOptions(IEnumerable<Food> foods)
    {
        return foods.Select(food => new FoodOptionDto(food.Id, food.Name)).ToList();
    }

    private static MenuEditorMenuDto ToMenuEditorMenuDto(string code, MenuEditorNoteState? editorState, Menu? menu)
    {
        if (editorState?.Menus.TryGetValue(code, out var configuredMenu) == true)
        {
            return configuredMenu;
        }

        var foods = menu?.MenuItems.OrderBy(item => item.CourseOrder).Select(item => item.FoodId).ToList() ?? [];

        return new MenuEditorMenuDto(
            code,
            foods.ElementAtOrDefault(0),
            foods.ElementAtOrDefault(1),
            foods.ElementAtOrDefault(2));
    }

    private static PlusMenuDto? ToPlusMenuDto(MenuEditorNoteState? editorState, Menu? menu)
    {
        var foodId = editorState?.Plus?.FoodId ??
            menu?.MenuItems.OrderBy(item => item.CourseOrder).Select(item => item.FoodId).FirstOrDefault();

        return foodId is null or 0 ? null : new PlusMenuDto(foodId.Value);
    }

    private static MenuEditorNoteState? ReadMenuEditorState(string? note)
    {
        if (string.IsNullOrWhiteSpace(note) || !note.StartsWith(MenuEditorNotePrefix, StringComparison.Ordinal))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<MenuEditorNoteState>(note[MenuEditorNotePrefix.Length..]);
        }
        catch
        {
            return null;
        }
    }

    private static string WriteMenuEditorState(SaveMenuDayRequest day)
    {
        var state = new MenuEditorNoteState
        {
            Menus = day.Menus.ToDictionary(item => item.Code, StringComparer.OrdinalIgnoreCase),
            Plus = day.Plus
        };

        return MenuEditorNotePrefix + JsonSerializer.Serialize(state);
    }

    private static FoodListItemDto ToFoodListItemDto(Food food)
    {
        return new FoodListItemDto(
            food.Id,
            food.Name,
            new LookupDto(food.CategoryId, food.Category.Name),
            (int?)food.Kcal,
            food.MenuItems.Count > 0,
            string.Join(Environment.NewLine, food.RecipeItems.OrderBy(item => item.Ingredient.Name).Select(item => $"{item.Ingredient.Name} {item.Amount:g} {item.Unit}".Trim())),
            food.RecipeItems.OrderBy(item => item.Ingredient.Name).Select(item => $"{item.Ingredient.Name} {item.Amount:g} {item.Unit}".Trim()).ToList(),
            food.Allergens.OrderBy(item => item.Name).Select(item => new LookupDto(item.Id, item.Name)).ToList());
    }

    private static bool IsCategory(string categoryName, string expected)
    {
        var normalized = categoryName.ToLowerInvariant();
        return expected switch
        {
            "leves" => normalized.Contains("leves"),
            "foetel" => normalized.Contains("etel") || normalized.Contains("fő"),
            "koret" => normalized.Contains("ret") || normalized.Contains("kö"),
            "salata" => normalized.Contains("sal"),
            "desszert" => normalized.Contains("desszert"),
            _ => false
        };
    }

    private static DateOnly GetWeekStart(DateOnly date)
    {
        var difference = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-difference);
    }

    private static string ToHungarianDayName(DateOnly date)
    {
        var dayName = HungarianCulture.DateTimeFormat.GetDayName(date.DayOfWeek);
        return char.ToUpper(dayName[0], HungarianCulture) + dayName[1..];
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

public sealed record MenuWeekOptionDto(DateOnly WeekStart, DateOnly WeekEnd, string Label, bool IsCurrentWeek, bool IsNextWeek);
public sealed record FoodOptionDto(ulong Id, string Name);
public sealed record MenuEditorOptionsDto(IReadOnlyList<FoodOptionDto> Starters, IReadOnlyList<FoodOptionDto> MainCourses, IReadOnlyList<FoodOptionDto> SideDishesAndSalads, IReadOnlyList<FoodOptionDto> PlusDesserts);
public sealed record MenuWeekEditorDto(DateOnly WeekStart, IReadOnlyList<MenuEditorDayDto> Days);
public sealed record MenuEditorDayDto(DateOnly Date, string DayName, IReadOnlyList<MenuEditorMenuDto> Menus, PlusMenuDto? Plus);
public sealed record MenuEditorMenuDto(string Code, ulong StarterFoodId, ulong MainCourseFoodId, ulong SideDishFoodId);
public sealed record PlusMenuDto(ulong FoodId);
public sealed record SaveMenuWeekRequest(IReadOnlyList<SaveMenuDayRequest> Days);
public sealed record SaveMenuDayRequest(DateOnly Date, IReadOnlyList<MenuEditorMenuDto> Menus, PlusMenuDto? Plus);
public sealed class MenuEditorNoteState
{
    public Dictionary<string, MenuEditorMenuDto> Menus { get; set; } = [];

    public PlusMenuDto? Plus { get; set; }
}
public sealed record LookupDto(ulong Id, string Name);
public sealed record FoodReferenceDataDto(IReadOnlyList<LookupDto> Categories, IReadOnlyList<LookupDto> Allergens);
public sealed record FoodListItemDto(ulong Id, string Name, LookupDto Category, int? Calories, bool IsOnMenu, string Recipe, IReadOnlyList<string> Ingredients, IReadOnlyList<LookupDto> Allergens);
public sealed record CreateFoodRequest(string Name, uint CategoryId, int? Calories, string? Recipe, IReadOnlyList<string> Ingredients, IReadOnlyList<ulong> AllergenIds);
public sealed record OrderCustomerDto(ulong Id, string FullName, string? Address, string Email, string? Phone);
public sealed record OrderMenuDto(ulong Id, string Code, int UnitPrice);
public sealed record OrderCreateOptionsDto(IReadOnlyList<OrderCustomerDto> Customers, IReadOnlyList<OrderMenuDto> Menus);
public sealed record CreateCustomerRequest(string FullName, string Email, string? Phone, string? Address);
public sealed record AdminUserDto(ulong Id, string FullName, string? Address, string Email, string? Phone, bool? IsActive);
public sealed record AdminCreatedUserDto(ulong Id, string FullName, string? Address, string Email, string? Phone);
public sealed record AdminMenuDto(ulong Id, string Code, int UnitPrice, bool? IsActive);
public sealed record CreateAdminOrderRequest(
    ulong? CustomerId,
    CreateCustomerRequest? NewCustomer,
    ulong? MenuId,
    int? Quantity,
    DateOnly DeliveryDate,
    string? Comment,
    IReadOnlyList<CreateAdminOrderItemRequest>? Items = null);
public sealed record CreateAdminOrderItemRequest(ulong MenuId, int Quantity);
public sealed record UpdateAdminOrderRequest(string CustomerName, string Address, string? Phone, string Email, ulong MenuId, int Quantity, DateOnly DeliveryDate, string? Comment);
public sealed record OrderListItemDto(ulong OrderId, ulong MenuId, ulong CustomerId, string CustomerName, string? Address, string? Phone, string Email, DateOnly DeliveryDate, string MenuCode, int Quantity, int UnitPrice, string? Comment);
public sealed record TicketDto(ulong Id, string CustomerName, string TicketType, string Description, string Status);
public sealed record CreateAdminTicketRequest(ulong? UserId, CreateCustomerRequest? Customer, string? TicketTypeName, string Description);
