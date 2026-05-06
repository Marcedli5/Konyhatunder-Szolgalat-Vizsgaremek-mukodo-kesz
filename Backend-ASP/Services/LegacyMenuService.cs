using Konyhatunder_Szolgalat_Vizsgaremek.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Backend_ASP.Services
{
    public class LegacyMenuService
    {
        private const string MenuEditorNotePrefix = "[WPF_MENU_EDITOR]";

        private readonly VizsgaremekEtlapContext _context;

        public LegacyMenuService(VizsgaremekEtlapContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<DailyMenuOfferDto>> GetUpcomingDailyMenusAsync(int days = 7)
        {
            var start = DateOnly.FromDateTime(DateTime.Today);
            var end = start.AddDays(days);
            return await GetDailyMenusAsync(start, end);
        }

        public async Task<IReadOnlyList<DailyMenuOfferDto>> GetDailyMenusAsync(DateOnly start, DateOnly end)
        {
            var dailyMenus = await _context.DailyMenus
                .AsNoTracking()
                .Include(dailyMenu => dailyMenu.MenuAvailabilities)
                    .ThenInclude(availability => availability.Menu)
                        .ThenInclude(menu => menu.MenuItems)
                            .ThenInclude(menuItem => menuItem.Food)
                .Where(dailyMenu => dailyMenu.MenuDate >= start && dailyMenu.MenuDate <= end)
                .OrderBy(dailyMenu => dailyMenu.MenuDate)
                .ToListAsync();

            var statesByDate = dailyMenus.ToDictionary(
                dailyMenu => dailyMenu.MenuDate,
                dailyMenu => ReadMenuEditorState(dailyMenu.Note));

            var dailyFoodIds = statesByDate.Values
                .Where(state => state is not null)
                .SelectMany(GetConfiguredFoodIds)
                .Distinct()
                .ToList();

            var dailyFoods = dailyFoodIds.Count == 0
                ? new Dictionary<ulong, string>()
                : await _context.Foods
                    .AsNoTracking()
                    .Where(food => dailyFoodIds.Contains(food.Id))
                    .ToDictionaryAsync(food => food.Id, food => food.Name);

            return dailyMenus
                .Select(dailyMenu => new DailyMenuOfferDto(
                    dailyMenu.MenuDate,
                    GetPublicNote(dailyMenu.Note),
                    dailyMenu.MenuAvailabilities
                        .Where(availability => IsPublicMenuCode(availability.Menu.Code) && availability.Menu.IsActive != false)
                        .OrderBy(availability => availability.Menu.Code)
                        .Select(availability => CreateMenuOfferDto(
                            availability.MenuId,
                            availability.Menu,
                            availability.MaxQty,
                            dailyMenu.MenuDate,
                            statesByDate.GetValueOrDefault(dailyMenu.MenuDate),
                            dailyFoods))
                        .ToList()))
                .ToList();
        }

        public async Task<MenuOfferDto?> GetMenuOfferAsync(ulong menuId, DateOnly deliveryDate)
        {
            var dailyMenu = await _context.DailyMenus
                .AsNoTracking()
                .Include(item => item.MenuAvailabilities)
                    .ThenInclude(availability => availability.Menu)
                        .ThenInclude(menu => menu.MenuItems)
                            .ThenInclude(menuItem => menuItem.Food)
                .FirstOrDefaultAsync(item => item.MenuDate == deliveryDate);

            var availabilityForMenu = dailyMenu?.MenuAvailabilities.FirstOrDefault(availability =>
                availability.MenuId == menuId &&
                IsPublicMenuCode(availability.Menu.Code) &&
                availability.Menu.IsActive != false);
            if (availabilityForMenu is null)
            {
                return null;
            }

            var editorState = ReadMenuEditorState(dailyMenu?.Note);
            var foodIds = editorState is null
                ? []
                : GetConfiguredFoodIds(editorState).Distinct().ToList();

            var foods = foodIds.Count == 0
                ? new Dictionary<ulong, string>()
                : await _context.Foods
                    .AsNoTracking()
                    .Where(food => foodIds.Contains(food.Id))
                    .ToDictionaryAsync(food => food.Id, food => food.Name);

            return CreateMenuOfferDto(
                availabilityForMenu.MenuId,
                availabilityForMenu.Menu,
                availabilityForMenu.MaxQty,
                deliveryDate,
                editorState,
                foods);
        }

        private static MenuOfferDto CreateMenuOfferDto(
            ulong menuId,
            Menu menu,
            uint? maxQuantity,
            DateOnly deliveryDate,
            MenuEditorNoteState? editorState,
            IReadOnlyDictionary<ulong, string> dailyFoods)
        {
            var configuredFoods = BuildConfiguredFoods(menu.Code, editorState, dailyFoods);
            var foods = configuredFoods.Count > 0
                ? configuredFoods
                : menu.MenuItems
                    .OrderBy(menuItem => menuItem.CourseOrder)
                    .Select(menuItem => new MenuOfferFoodDto(menuItem.CourseOrder, menuItem.Food.Name))
                    .ToList();

            return new MenuOfferDto(
                menuId,
                menu.Code,
                menu.Code == "P" ? "Plusz ajánlat" : $"{menu.Code} menü",
                (int)menu.PriceFt,
                (int)(maxQuantity ?? 0),
                deliveryDate,
                foods);
        }

        private static IReadOnlyList<ulong> GetConfiguredFoodIds(MenuEditorNoteState? editorState)
        {
            if (editorState is null)
            {
                return [];
            }

            var menuFoods = editorState.Menus.Values.SelectMany(menu => new[]
            {
                menu.StarterFoodId,
                menu.MainCourseFoodId,
                menu.SideDishFoodId
            });

            return editorState.Plus is { } plus
                ? menuFoods.Append(plus.FoodId).ToList()
                : menuFoods.ToList();
        }

        private static List<MenuOfferFoodDto> BuildConfiguredFoods(
            string menuCode,
            MenuEditorNoteState? editorState,
            IReadOnlyDictionary<ulong, string> dailyFoods)
        {
            var foodIds = GetConfiguredFoodIds(menuCode, editorState);
            var result = new List<MenuOfferFoodDto>();

            for (var index = 0; index < foodIds.Count; index++)
            {
                if (dailyFoods.TryGetValue(foodIds[index], out var name))
                {
                    result.Add(new MenuOfferFoodDto((byte)(index + 1), name));
                }
            }

            return result;
        }

        private static IReadOnlyList<ulong> GetConfiguredFoodIds(string menuCode, MenuEditorNoteState? editorState)
        {
            if (editorState is null)
            {
                return [];
            }

            if (menuCode == "P")
            {
                return editorState.Plus is { } plus ? [plus.FoodId] : [];
            }

            return editorState.Menus.TryGetValue(menuCode, out var menu)
                ? [menu.StarterFoodId, menu.MainCourseFoodId, menu.SideDishFoodId]
                : [];
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

        private static string? GetPublicNote(string? note)
        {
            return string.IsNullOrWhiteSpace(note) || note.StartsWith(MenuEditorNotePrefix, StringComparison.Ordinal)
                ? null
                : note;
        }

        private static bool IsPublicMenuCode(string code)
        {
            return code is "A" or "B" or "C" or "P";
        }
    }

    public record DailyMenuOfferDto(DateOnly DeliveryDate, string? Note, IReadOnlyList<MenuOfferDto> Offers);

    public record MenuOfferDto(
        ulong MenuId,
        string MenuCode,
        string DisplayName,
        int UnitPriceFt,
        int MaxQuantity,
        DateOnly DeliveryDate,
        IReadOnlyList<MenuOfferFoodDto> Foods);

    public record MenuOfferFoodDto(byte CourseOrder, string Name);

    public sealed class MenuEditorNoteState
    {
        public Dictionary<string, MenuEditorNoteMenu> Menus { get; set; } = [];

        public MenuEditorNotePlus? Plus { get; set; }
    }

    public sealed record MenuEditorNoteMenu(string Code, ulong StarterFoodId, ulong MainCourseFoodId, ulong SideDishFoodId);

    public sealed record MenuEditorNotePlus(ulong FoodId);
}
