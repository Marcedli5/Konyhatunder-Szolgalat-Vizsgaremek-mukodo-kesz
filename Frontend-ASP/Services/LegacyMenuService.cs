namespace Frontend_ASP.Services
{
    public class LegacyMenuService(BackendApiClient backendApiClient)
    {
        private readonly BackendApiClient _backendApiClient = backendApiClient;

        public async Task<IReadOnlyList<DailyMenuOfferDto>> GetUpcomingDailyMenusAsync(int days = 7)
            => await GetDailyMenusAsync(DateOnly.FromDateTime(DateTime.Today), days);

        public async Task<IReadOnlyList<DailyMenuOfferDto>> GetDailyMenusAsync(DateOnly from, int days)
        {
            var dailyMenus = await _backendApiClient.GetAsync<IReadOnlyList<DailyMenuOfferDto>>($"api/menu/upcoming?from={from:yyyy-MM-dd}&days={days}") ?? [];
            return dailyMenus
                .Select(day => day with
                {
                    Offers = day.Offers
                        .Where(offer => IsPublicMenuCode(offer.MenuCode))
                        .OrderBy(offer => offer.MenuCode)
                        .ToList()
                })
                .ToList();
        }

        public async Task<MenuOfferDto?> GetMenuOfferAsync(ulong menuId, DateOnly deliveryDate)
        {
            var dailyMenus = await GetUpcomingDailyMenusAsync(14);
            return dailyMenus
                .FirstOrDefault(day => day.DeliveryDate == deliveryDate)?
                .Offers
                .FirstOrDefault(offer => offer.MenuId == menuId && IsPublicMenuCode(offer.MenuCode));
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
}
