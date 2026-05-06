using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using WPF_AdminFelulet.Commands;
using WPF_AdminFelulet.Services;

namespace WPF_AdminFelulet.ViewModels;

public sealed class EtelekViewModel : TabViewModelBase
{
    private bool _ideiglenesAdatokEngedelyezve = false;

    private readonly IAdminOrdersApiClient _apiClient;
    private readonly RelayCommand _addFoodCommand;
    private readonly RelayCommand _addAllergenCommand;
    private readonly RelayCommand _removeAllergenCommand;
    private bool _isLoading;
    private bool _isSaving;
    private bool _isUpdatingCategoryFilters;
    private string _newFoodName = string.Empty;
    private string _newRecipe = string.Empty;
    private string _newIngredientsText = string.Empty;
    private string _newCaloriesText = string.Empty;
    private string _foodSearchText = string.Empty;
    private FoodCategoryOptionViewModel? _selectedCategory;
    private AllergenOptionViewModel? _selectedAvailableAllergen;
    private FoodListItemViewModel? _selectedFood;

    public EtelekViewModel(IAdminOrdersApiClient apiClient)
        : base(
            "Ételek",
            "Étel törzsadatok kezelése validált felvitellel.")
    {
        _apiClient = apiClient;
        Categories = [];
        AvailableAllergens = [];
        SelectedAllergens = [];
        CategoryFilters = [];
        UserFoods = [];
        FilteredFoods = [];

        _addFoodCommand = new RelayCommand(_ => _ = AddFoodAsync(), _ => IsFormValid);
        _addAllergenCommand = new RelayCommand(_ => AddSelectedAllergen(), _ => CanAddSelectedAllergen);
        _removeAllergenCommand = new RelayCommand(
            allergen => RemoveSelectedAllergen(allergen as AllergenOptionViewModel),
            allergen => allergen is AllergenOptionViewModel);

        AddFoodCommand = _addFoodCommand;
        AddAllergenCommand = _addAllergenCommand;
        RemoveAllergenCommand = _removeAllergenCommand;

        _ = LoadInitialDataAsync();
    }

    public ObservableCollection<FoodCategoryOptionViewModel> Categories { get; }

    public ObservableCollection<AllergenOptionViewModel> AvailableAllergens { get; }

    public ObservableCollection<AllergenOptionViewModel> SelectedAllergens { get; }

    public ObservableCollection<CategoryFilterOptionViewModel> CategoryFilters { get; }

    public ObservableCollection<FoodListItemViewModel> UserFoods { get; }

    public ObservableCollection<FoodListItemViewModel> FilteredFoods { get; }

    public ICommand AddFoodCommand { get; }

    public ICommand AddAllergenCommand { get; }

    public ICommand RemoveAllergenCommand { get; }

    public string NewFoodName
    {
        get => _newFoodName;
        set
        {
            if (SetProperty(ref _newFoodName, value))
            {
                RefreshFormState();
            }
        }
    }

    public FoodCategoryOptionViewModel? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                RefreshFormState();
            }
        }
    }

    public string NewRecipe
    {
        get => _newRecipe;
        set
        {
            if (SetProperty(ref _newRecipe, value))
            {
                RefreshFormState();
            }
        }
    }

    public string NewIngredientsText
    {
        get => _newIngredientsText;
        set
        {
            if (SetProperty(ref _newIngredientsText, value))
            {
                OnPropertyChanged(nameof(IngredientHintText));
                RefreshFormState();
            }
        }
    }

    public string NewCaloriesText
    {
        get => _newCaloriesText;
        set => SetProperty(ref _newCaloriesText, value);
    }

    public AllergenOptionViewModel? SelectedAvailableAllergen
    {
        get => _selectedAvailableAllergen;
        set
        {
            if (SetProperty(ref _selectedAvailableAllergen, value))
            {
                RefreshFormState();
            }
        }
    }

    public string FoodSearchText
    {
        get => _foodSearchText;
        set
        {
            if (SetProperty(ref _foodSearchText, value))
            {
                ApplyFilters();
            }
        }
    }

    public FoodListItemViewModel? SelectedFood
    {
        get => _selectedFood;
        set
        {
            if (SetProperty(ref _selectedFood, value))
            {
                OnPropertyChanged(nameof(HasSelectedFood));
            }
        }
    }

    public bool HasSelectedFood => SelectedFood is not null;

    public bool IsFormValid =>
        !_isLoading &&
        !_isSaving &&
        !string.IsNullOrWhiteSpace(NewFoodName) &&
        SelectedCategory is not null &&
        !string.IsNullOrWhiteSpace(NewRecipe) &&
        ParseIngredients(NewIngredientsText).Count > 0;

    public bool CanAddSelectedAllergen =>
        SelectedAvailableAllergen is not null &&
        !SelectedAllergens.Any(x => x.Id == SelectedAvailableAllergen.Id);

    public string IngredientHintText => ParseIngredients(NewIngredientsText).Count switch
    {
        0 => "Legalább 1 hozzávaló megadása kötelező, soronként egy tétellel.",
        1 => "1 hozzávaló lett felismerve.",
        _ => $"{ParseIngredients(NewIngredientsText).Count} hozzávaló lett felismerve."
    };

    public string VisibleFoodCountText => $"{FilteredFoods.Count} / {UserFoods.Count} étel látható";

    private async Task LoadInitialDataAsync()
    {
        try
        {
            _isLoading = true;
            RefreshFormState();

            if (_ideiglenesAdatokEngedelyezve)
            {
                LoadTemporaryReferenceData();
                LoadTemporaryFoods();
                return;
            }

            var referenceData = await _apiClient.GetFoodReferenceDataAsync();
            Categories.Clear();
            foreach (var category in referenceData.Categories.OrderBy(item => item.Name))
            {
                Categories.Add(new FoodCategoryOptionViewModel(category.Id, category.Name));
            }

            AvailableAllergens.Clear();
            foreach (var allergen in referenceData.Allergens.OrderBy(item => item.Name))
            {
                AvailableAllergens.Add(new AllergenOptionViewModel(allergen.Id, allergen.Name));
            }

            BuildCategoryFilters();
            await LoadFoodsAsync();
        }
        catch (Exception)
        {
            MessageBox.Show(
                "Az ételek adatainak betöltése nem sikerült. Ellenőrizd, hogy fut-e a Backend.",
                "Ételek",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        finally
        {
            _isLoading = false;
            RefreshFormState();
        }
    }

    private async Task LoadFoodsAsync()
    {
        var foods = await _apiClient.GetFoodsAsync();
        UserFoods.Clear();
        foreach (var food in foods.Select(ToViewModel).OrderBy(item => item.Name))
        {
            UserFoods.Add(food);
        }

        ApplyFilters();
    }

    private void LoadTemporaryReferenceData()
    {
        Categories.Clear();
        Categories.Add(new FoodCategoryOptionViewModel(1, "Leves"));
        Categories.Add(new FoodCategoryOptionViewModel(2, "Főétel"));
        Categories.Add(new FoodCategoryOptionViewModel(3, "Köret"));
        Categories.Add(new FoodCategoryOptionViewModel(4, "Saláta"));
        Categories.Add(new FoodCategoryOptionViewModel(5, "Desszert"));

        AvailableAllergens.Clear();
        AvailableAllergens.Add(new AllergenOptionViewModel(1, "Glutén"));
        AvailableAllergens.Add(new AllergenOptionViewModel(2, "Tej"));
        AvailableAllergens.Add(new AllergenOptionViewModel(3, "Tojás"));
        AvailableAllergens.Add(new AllergenOptionViewModel(4, "Diófélék"));

        BuildCategoryFilters();
    }

    private void LoadTemporaryFoods()
    {
        UserFoods.Clear();

        var leves = Categories.First(category => category.Name == "Leves");
        var foetel = Categories.First(category => category.Name == "Főétel");
        var koret = Categories.First(category => category.Name == "Köret");
        var desszert = Categories.First(category => category.Name == "Desszert");
        var gluten = AvailableAllergens.First(allergen => allergen.Name == "Glutén");
        var tej = AvailableAllergens.First(allergen => allergen.Name == "Tej");
        var tojas = AvailableAllergens.First(allergen => allergen.Name == "Tojás");

        UserFoods.Add(new FoodListItemViewModel(1, "Húsleves", leves, 180, true, "A zöldségeket és a húst lassú tűzön puhára főzzük.", [new IngredientLineViewModel("csirkehús"), new IngredientLineViewModel("sárgarépa"), new IngredientLineViewModel("petrezselyemgyökér")], []));
        UserFoods.Add(new FoodListItemViewModel(2, "Rántott csirkemell", foetel, 520, true, "A csirkemellet panírozzuk, majd bő olajban kisütjük.", [new IngredientLineViewModel("csirkemell"), new IngredientLineViewModel("liszt"), new IngredientLineViewModel("tojás"), new IngredientLineViewModel("zsemlemorzsa")], [gluten, tojas]));
        UserFoods.Add(new FoodListItemViewModel(3, "Petrezselymes burgonya", koret, 260, true, "A főtt burgonyát petrezselyemmel átforgatjuk.", [new IngredientLineViewModel("burgonya"), new IngredientLineViewModel("petrezselyem"), new IngredientLineViewModel("vaj")], [tej]));
        UserFoods.Add(new FoodListItemViewModel(4, "Túrógombóc", desszert, 430, true, "A túrós masszából gombócokat főzünk, majd morzsába forgatjuk.", [new IngredientLineViewModel("túró"), new IngredientLineViewModel("búzadara"), new IngredientLineViewModel("tojás")], [tej, tojas]));

        ApplyFilters();
    }

    private void BuildCategoryFilters()
    {
        CategoryFilters.Clear();
        CategoryFilters.Add(new CategoryFilterOptionViewModel("Mind", null, SelectCategoryFilter));

        foreach (var category in Categories)
        {
            CategoryFilters.Add(new CategoryFilterOptionViewModel(category.Name, category.Id, SelectCategoryFilter));
        }

        SelectCategoryFilter(CategoryFilters[0]);
    }

    private void SelectCategoryFilter(CategoryFilterOptionViewModel selectedFilter)
    {
        if (_isUpdatingCategoryFilters)
        {
            return;
        }

        _isUpdatingCategoryFilters = true;
        foreach (var filter in CategoryFilters)
        {
            filter.SetSelectionSilently(ReferenceEquals(filter, selectedFilter));
        }

        selectedFilter.SetSelectionSilently(true);
        _isUpdatingCategoryFilters = false;
        ApplyFilters();
    }

    private void AddSelectedAllergen()
    {
        if (SelectedAvailableAllergen is null || !CanAddSelectedAllergen)
        {
            return;
        }

        SelectedAllergens.Add(new AllergenOptionViewModel(SelectedAvailableAllergen.Id, SelectedAvailableAllergen.Name));
        SelectedAvailableAllergen = null;
        RefreshFormState();
    }

    private void RemoveSelectedAllergen(AllergenOptionViewModel? allergen)
    {
        if (allergen is null)
        {
            return;
        }

        SelectedAllergens.Remove(allergen);
        RefreshFormState();
    }

    private async Task AddFoodAsync()
    {
        var ingredients = ParseIngredientTexts(NewIngredientsText);

        if (!IsFormValid || SelectedCategory is null)
        {
            return;
        }

        try
        {
            _isSaving = true;
            RefreshFormState();

            if (_ideiglenesAdatokEngedelyezve)
            {
                var temporaryFood = new FoodListItemViewModel(
                    (ulong)(UserFoods.Count == 0 ? 1 : UserFoods.Max(item => (long)item.Id) + 1),
                    NewFoodName.Trim(),
                    SelectedCategory,
                    int.TryParse(NewCaloriesText, out var parsedTemporaryCalories) ? parsedTemporaryCalories : null,
                    false,
                    NewRecipe.Trim(),
                    ingredients.Select(item => new IngredientLineViewModel(item)).ToList(),
                    SelectedAllergens.ToList());

                UserFoods.Insert(0, temporaryFood);
                ApplyFilters();
                SelectedFood = temporaryFood;

                MessageBox.Show(
                    "Az étel ideiglenes adatokkal lett rögzítve, ezért csak a felületen marad meg.",
                    "Étel rögzítve",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                ResetForm();
                return;
            }

            var createdFood = await _apiClient.CreateFoodAsync(new AdminCreateFoodRequest
            {
                Name = NewFoodName.Trim(),
                CategoryId = checked((uint)SelectedCategory.Id),
                Calories = int.TryParse(NewCaloriesText, out var parsedCalories) ? parsedCalories : null,
                Recipe = NewRecipe.Trim(),
                Ingredients = ingredients,
                AllergenIds = SelectedAllergens.Select(item => item.Id).ToList()
            });

            var food = ToViewModel(createdFood);
            UserFoods.Insert(0, food);
            ApplyFilters();
            SelectedFood = FilteredFoods.FirstOrDefault(x => x.Id == food.Id) ?? food;

            MessageBox.Show(
                "Az étel sikeresen rögzült a Backenden.",
                "Étel rögzítve",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            ResetForm();
        }
        catch (Exception ex)
        {
            var details = ex is AdminApiException apiException
                ? apiException.Message
                : ex.Message;

            MessageBox.Show(
                $"Az étel mentése nem sikerült.\n\n{details}",
                "Étel rögzítése",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            _isSaving = false;
            RefreshFormState();
        }
    }

    private void ResetForm()
    {
        NewFoodName = string.Empty;
        SelectedCategory = null;
        NewRecipe = string.Empty;
        NewIngredientsText = string.Empty;
        NewCaloriesText = string.Empty;
        SelectedAvailableAllergen = null;
        SelectedAllergens.Clear();
        RefreshFormState();
    }

    private void ApplyFilters()
    {
        var selectedCategoryId = CategoryFilters.FirstOrDefault(x => x.IsSelected)?.CategoryId;
        var searchText = FoodSearchText.Trim();

        var filtered = UserFoods
            .Where(food =>
                (string.IsNullOrWhiteSpace(searchText) ||
                 food.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)) &&
                (!selectedCategoryId.HasValue || food.Category.Id == selectedCategoryId.Value))
            .OrderBy(food => food.Name)
            .ToList();

        FilteredFoods.Clear();
        foreach (var food in filtered)
        {
            FilteredFoods.Add(food);
        }

        if (SelectedFood is not null && !FilteredFoods.Contains(SelectedFood))
        {
            SelectedFood = FilteredFoods.FirstOrDefault();
        }
        else if (SelectedFood is null)
        {
            SelectedFood = FilteredFoods.FirstOrDefault();
        }

        OnPropertyChanged(nameof(VisibleFoodCountText));
    }

    private void RefreshFormState()
    {
        OnPropertyChanged(nameof(IsFormValid));
        OnPropertyChanged(nameof(CanAddSelectedAllergen));
        OnPropertyChanged(nameof(IngredientHintText));
        _addFoodCommand.RaiseCanExecuteChanged();
        _addAllergenCommand.RaiseCanExecuteChanged();
    }

    private List<IngredientLineViewModel> ParseIngredients(string rawIngredients)
    {
        return ParseIngredientTexts(rawIngredients)
            .Select(line => new IngredientLineViewModel(line))
            .ToList();
    }

    private static List<string> ParseIngredientTexts(string rawIngredients)
    {
        return rawIngredients
            .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private FoodListItemViewModel ToViewModel(AdminFoodListItemDto food)
    {
        return new FoodListItemViewModel(
            food.Id,
            food.Name,
            new FoodCategoryOptionViewModel(food.Category.Id, food.Category.Name),
            food.Calories,
            food.IsOnMenu,
            food.Recipe,
            food.Ingredients.Select(item => new IngredientLineViewModel(item)).ToList(),
            food.Allergens.Select(item => new AllergenOptionViewModel(item.Id, item.Name)).ToList());
    }
}

public sealed class FoodCategoryOptionViewModel
{
    public FoodCategoryOptionViewModel(ulong id, string name)
    {
        Id = id;
        Name = name;
    }

    public ulong Id { get; }

    public string Name { get; }
}

public sealed class AllergenOptionViewModel
{
    public AllergenOptionViewModel(ulong id, string name)
    {
        Id = id;
        Name = name;
    }

    public ulong Id { get; }

    public string Name { get; }
}

public sealed class CategoryFilterOptionViewModel : ViewModelBase
{
    private readonly Action<CategoryFilterOptionViewModel> _onSelected;
    private bool _isSelected;

    public CategoryFilterOptionViewModel(string label, ulong? categoryId, Action<CategoryFilterOptionViewModel> onSelected)
    {
        Label = label;
        CategoryId = categoryId;
        _onSelected = onSelected;
    }

    public string Label { get; }

    public ulong? CategoryId { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                if (value)
                {
                    _onSelected(this);
                }
            }
        }
    }

    public void SetSelectionSilently(bool isSelected)
    {
        SetProperty(ref _isSelected, isSelected, nameof(IsSelected));
    }
}

public sealed class IngredientLineViewModel
{
    public IngredientLineViewModel(string displayText)
    {
        DisplayText = displayText;
    }

    public string DisplayText { get; }
}

public sealed class FoodListItemViewModel
{
    public FoodListItemViewModel(
        ulong id,
        string name,
        FoodCategoryOptionViewModel category,
        int? calories,
        bool isOnMenu,
        string recipe,
        IReadOnlyList<IngredientLineViewModel> ingredients,
        IReadOnlyList<AllergenOptionViewModel> allergens)
    {
        Id = id;
        Name = name;
        Category = category;
        Calories = calories;
        IsOnMenu = isOnMenu;
        Recipe = recipe;
        Ingredients = ingredients.ToList();
        Allergens = allergens.ToList();
    }

    public ulong Id { get; }

    public string Name { get; }

    public FoodCategoryOptionViewModel Category { get; }

    public int? Calories { get; }

    public bool IsOnMenu { get; }

    public string Recipe { get; }

    public IReadOnlyList<IngredientLineViewModel> Ingredients { get; }

    public IReadOnlyList<AllergenOptionViewModel> Allergens { get; }

    public string CategoryName => Category.Name;

    public string CaloriesText => Calories.HasValue ? $"{Calories.Value} kcal" : "Kcal nincs megadva";

    public string MenuStateText => IsOnMenu ? "Már használva menüben" : "Még nincs menübe rakva";

    public string AllergenSummary => Allergens.Count == 0
        ? "Nincs megadott allergén"
        : string.Join(", ", Allergens.Select(x => x.Name));

    public string IngredientSummary => Ingredients.Count == 0
        ? "Nincs hozzávaló"
        : string.Join(", ", Ingredients.Select(x => x.DisplayText));
}
