using Frontend_ASP.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;

namespace Frontend_ASP.Pages
{
    public class EtlapModel : PageModel
    {
        private readonly LegacyMenuService _legacyMenuService;
        private readonly CultureInfo _hungarianCulture = new("hu-HU");

        public EtlapModel(LegacyMenuService legacyMenuService)
        {
            _legacyMenuService = legacyMenuService;
        }

        public IReadOnlyList<MenuWeekSection> WeekSections { get; private set; } = [];

        public bool CanOrder => User.Identity?.IsAuthenticated == true;

        public async Task OnGetAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var currentWeekStart = GetWeekStart(today);
            var nextWeekStart = currentWeekStart.AddDays(7);
            var nextWeekEnd = nextWeekStart.AddDays(5);
            var requestDays = nextWeekEnd.DayNumber - currentWeekStart.DayNumber;
            var dailyMenus = await _legacyMenuService.GetDailyMenusAsync(currentWeekStart, requestDays);
            var menusByDate = dailyMenus.ToDictionary(day => day.DeliveryDate);

            WeekSections =
            [
                BuildWeekSection("Aktuális hét", currentWeekStart, menusByDate),
                BuildWeekSection("Következő hét", nextWeekStart, menusByDate)
            ];
        }

        private MenuWeekSection BuildWeekSection(
            string title,
            DateOnly weekStart,
            IReadOnlyDictionary<DateOnly, DailyMenuOfferDto> menusByDate)
        {
            var days = Enumerable.Range(0, 6)
                .Select(index =>
                {
                    var date = weekStart.AddDays(index);
                    return menusByDate.TryGetValue(date, out var day)
                        ? day
                        : new DailyMenuOfferDto(date, null, []);
                })
                .ToList();

            var weekEnd = weekStart.AddDays(5);
            var label = $"{weekStart.ToString("yyyy. MM. dd.", _hungarianCulture)} - {weekEnd.ToString("MM. dd.", _hungarianCulture)}";

            return new MenuWeekSection(title, label, days);
        }

        private static DateOnly GetWeekStart(DateOnly date)
        {
            var difference = ((int)date.DayOfWeek + 6) % 7;
            return date.AddDays(-difference);
        }
    }

    public record MenuWeekSection(string Title, string DateRange, IReadOnlyList<DailyMenuOfferDto> Days);
}
