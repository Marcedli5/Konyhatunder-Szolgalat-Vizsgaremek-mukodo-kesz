using System.ComponentModel.DataAnnotations;
using Frontend_ASP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Frontend_ASP.Pages.Admin
{
    [Authorize(Policy = "AdminOnly")]
    public class EtlapSzerkesztesModel(BackendApiClient backendApiClient) : PageModel
    {
        private readonly BackendApiClient _backendApiClient = backendApiClient;

        [BindProperty]
        public MenuItemInputModel NewItem { get; set; } = new();

        public List<MenuItem> Items { get; private set; } = [];

        [TempData]
        public string? StatusMessage { get; set; }

        public async Task OnGetAsync()
        {
            await LoadItemsAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadItemsAsync();
                return Page();
            }

            await _backendApiClient.PostAsync(
                "api/admin/foods",
                new CreateFoodRequest(
                    NewItem.Name.Trim(),
                    1,
                    null,
                    NewItem.Description,
                    string.IsNullOrWhiteSpace(NewItem.Description) ? [] : [NewItem.Description.Trim()],
                    []));

            StatusMessage = "Az új étel rögzítését elküldtük a Backend API-nak.";
            return RedirectToPage();
        }

        public IActionResult OnPostUpdate(int id, string category, string name, string? description, decimal price, int displayOrder, bool isAvailable)
        {
            StatusMessage = "A webes étlapszerkesztés közvetlen adatbázis nélkül fut. Módosításhoz használd a WPF admin étlap felületet.";
            return RedirectToPage();
        }

        public IActionResult OnPostDelete(int id)
        {
            StatusMessage = "A webes étlapszerkesztés közvetlen adatbázis nélkül fut. Törléshez használd a WPF admin étlap felületet.";
            return RedirectToPage();
        }

        private async Task LoadItemsAsync()
        {
            var foods = await _backendApiClient.GetAsync<IReadOnlyList<FoodListItemDto>>("api/admin/foods") ?? [];
            Items = foods
                .OrderBy(item => item.Category.Name)
                .ThenBy(item => item.Name)
                .Select(item => new MenuItem(
                    (int)item.Id,
                    item.Category.Name,
                    item.Name,
                    item.Recipe,
                    0,
                    0,
                    true))
                .ToList();
        }

        public class MenuItemInputModel
        {
            [Display(Name = "Kategória")]
            [Required(ErrorMessage = "A kategória megadása kötelező.")]
            [StringLength(100)]
            public string Category { get; set; } = string.Empty;

            [Display(Name = "Étel neve")]
            [Required(ErrorMessage = "Az étel neve kötelező.")]
            [StringLength(160)]
            public string Name { get; set; } = string.Empty;

            [Display(Name = "Leírás")]
            [StringLength(500)]
            public string? Description { get; set; }

            [Display(Name = "Ár")]
            [Range(0, 999999, ErrorMessage = "Az ár nem lehet negatív.")]
            public decimal Price { get; set; }

            [Display(Name = "Sorrend")]
            public int DisplayOrder { get; set; }
        }

        public sealed record MenuItem(int Id, string Category, string Name, string? Description, decimal Price, int DisplayOrder, bool IsAvailable);

        private sealed record LookupDto(ulong Id, string Name);

        private sealed record FoodListItemDto(
            ulong Id,
            string Name,
            LookupDto Category,
            int? Calories,
            bool IsOnMenu,
            string Recipe,
            IReadOnlyList<string> Ingredients,
            IReadOnlyList<LookupDto> Allergens);

        private sealed record CreateFoodRequest(
            string Name,
            uint CategoryId,
            int? Calories,
            string? Recipe,
            IReadOnlyList<string> Ingredients,
            IReadOnlyList<ulong> AllergenIds);
    }
}
