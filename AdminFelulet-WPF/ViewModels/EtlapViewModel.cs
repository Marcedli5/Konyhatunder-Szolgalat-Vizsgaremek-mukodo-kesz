using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using WPF_AdminFelulet.Commands;
using WPF_AdminFelulet.Services;

namespace WPF_AdminFelulet.ViewModels;

public sealed class EtlapViewModel : TabViewModelBase
{
    private bool _ideiglenesAdatokEngedelyezve = false;

    private readonly IAdminOrdersApiClient _apiClient;
    private readonly CultureInfo _culture = new("hu-HU");
    private readonly RelayCommand _saveCommand;
    private readonly RelayCommand _refreshCommand;
    private WeekOptionViewModel? _selectedWeek;
    private bool _isInternalSelectionChange;
    private bool _hasUnsavedChanges;
    private bool _isLoading;
    private bool _isSaving;

    public EtlapViewModel(IAdminOrdersApiClient apiClient)
        : base(
            "Étlap",
            "Heti étlapszerkesztő ideiglenes adatokkal vagy Backend API kapcsolattal.")
    {
        _apiClient = apiClient;
        AvailableWeeks = [];
        StarterOptions = [];
        MainCourseOptions = [];
        SideDishOptions = [];
        PlusOptions = [];
        MenuDays = [];

        _saveCommand = new RelayCommand(_ => _ = SaveMenuAsync(), _ => !_isLoading && !_isSaving);
        _refreshCommand = new RelayCommand(_ => _ = RefreshSelectedWeekAsync(), _ => !_isLoading);

        SaveCommand = _saveCommand;
        RefreshCommand = _refreshCommand;

        _ = LoadInitialDataAsync();
    }

    public ObservableCollection<WeekOptionViewModel> AvailableWeeks { get; }

    public ObservableCollection<FoodOptionViewModel> StarterOptions { get; }

    public ObservableCollection<FoodOptionViewModel> MainCourseOptions { get; }

    public ObservableCollection<FoodOptionViewModel> SideDishOptions { get; }

    public ObservableCollection<FoodOptionViewModel> PlusOptions { get; }

    public ObservableCollection<DailyMenuEditorViewModel> MenuDays { get; }

    public ICommand SaveCommand { get; }

    public ICommand RefreshCommand { get; }

    public WeekOptionViewModel? SelectedWeek
    {
        get => _selectedWeek;
        set
        {
            if (!SetProperty(ref _selectedWeek, value) || value is null)
            {
                return;
            }

            OnPropertyChanged(nameof(SelectedWeekCaption));
            _ = LoadWeekAsync(value);

            if (!_isInternalSelectionChange && value.IsNextWeek)
            {
                MessageBox.Show(
                    "A következő hét szerkeszthető új étlapként.",
                    "Étlap szerkesztés",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
    }

    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        set
        {
            if (SetProperty(ref _hasUnsavedChanges, value))
            {
                OnPropertyChanged(nameof(SaveButtonText));
            }
        }
    }

    public string SelectedWeekCaption => SelectedWeek is null
        ? "Válassz hetet."
        : $"Kiválasztott hét: {SelectedWeek.Label}";

    public string SaveButtonText => HasUnsavedChanges ? "Mentés" : "Mentve";

    private async Task LoadInitialDataAsync()
    {
        try
        {
            SetBusy(isLoading: true);
            ClearEditorData();

            if (_ideiglenesAdatokEngedelyezve)
            {
                LoadTemporaryOptions();
                BuildTemporaryWeeks();
            }
            else
            {
                await LoadMenuOptionsAsync();
                await LoadWeeksAsync();
            }

            SelectDefaultWeek();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Az étlap adatai nem érkeztek meg a Backendből. Ellenőrizd, hogy a Backend fut-e.\n\n{ex.Message}",
                "Étlap",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        finally
        {
            SetBusy(isLoading: false);
        }
    }

    private void ClearEditorData()
    {
        StarterOptions.Clear();
        MainCourseOptions.Clear();
        SideDishOptions.Clear();
        PlusOptions.Clear();
        AvailableWeeks.Clear();
        MenuDays.Clear();
        _selectedWeek = null;
        OnPropertyChanged(nameof(SelectedWeek));
        OnPropertyChanged(nameof(SelectedWeekCaption));
        HasUnsavedChanges = false;
    }

    private void LoadTemporaryOptions()
    {
        StarterOptions.Add(new FoodOptionViewModel(1, "Húsleves"));
        StarterOptions.Add(new FoodOptionViewModel(2, "Gulyásleves"));
        StarterOptions.Add(new FoodOptionViewModel(3, "Zöldségleves"));
        StarterOptions.Add(new FoodOptionViewModel(4, "Lencseleves"));

        MainCourseOptions.Add(new FoodOptionViewModel(101, "Rántott csirkemell"));
        MainCourseOptions.Add(new FoodOptionViewModel(102, "Sült csirkecomb"));
        MainCourseOptions.Add(new FoodOptionViewModel(103, "Bakonyi sertésszelet"));
        MainCourseOptions.Add(new FoodOptionViewModel(104, "Rakott karfiol"));
        MainCourseOptions.Add(new FoodOptionViewModel(105, "Csirkepaprikás"));
        MainCourseOptions.Add(new FoodOptionViewModel(201, "Túrógombóc"));
        MainCourseOptions.Add(new FoodOptionViewModel(202, "Mákos guba"));
        MainCourseOptions.Add(new FoodOptionViewModel(203, "Somlói galuska"));

        SideDishOptions.Add(new FoodOptionViewModel(301, "Petrezselymes burgonya"));
        SideDishOptions.Add(new FoodOptionViewModel(302, "Párolt rizs"));
        SideDishOptions.Add(new FoodOptionViewModel(303, "Burgonyapüré"));
        SideDishOptions.Add(new FoodOptionViewModel(401, "Cézár saláta"));
        SideDishOptions.Add(new FoodOptionViewModel(402, "Vegyes saláta"));
        SideDishOptions.Add(new FoodOptionViewModel(403, "Káposztasaláta"));
        SideDishOptions.Add(new FoodOptionViewModel(404, "Uborkasaláta"));

        PlusOptions.Add(new FoodOptionViewModel(501, "Túrógombóc"));
        PlusOptions.Add(new FoodOptionViewModel(502, "Mákos guba"));
        PlusOptions.Add(new FoodOptionViewModel(503, "Somlói galuska"));
        PlusOptions.Add(new FoodOptionViewModel(504, "Gesztenyepüré"));
    }

    private void BuildTemporaryWeeks()
    {
        var currentWeekStart = GetWeekStart(DateOnly.FromDateTime(DateTime.Today));

        for (var i = -3; i <= 1; i++)
        {
            var weekStart = currentWeekStart.AddDays(i * 7);
            var weekEnd = weekStart.AddDays(5);
            var isNextWeek = i == 1;
            var label = $"{weekStart:yyyy.MM.dd.} - {weekEnd:MM.dd.}";

            if (i == 0)
            {
                label += " | Aktuális hét";
            }

            if (isNextWeek)
            {
                label += " | Jövő hét";
            }

            AvailableWeeks.Add(new WeekOptionViewModel(weekStart, label, i == 0, isNextWeek));
        }
    }

    private async Task LoadMenuOptionsAsync()
    {
        var options = await _apiClient.GetMenuEditorOptionsAsync();
        ReplaceOptions(StarterOptions, options.Starters);
        ReplaceOptions(MainCourseOptions, options.MainCourses);
        ReplaceOptions(SideDishOptions, options.SideDishesAndSalads);
        ReplaceOptions(PlusOptions, options.PlusDesserts);
    }

    private async Task LoadWeeksAsync()
    {
        var weeks = await _apiClient.GetMenuWeeksAsync();
        AvailableWeeks.Clear();
        foreach (var week in weeks)
        {
            var label = week.Label;
            if (week.IsCurrentWeek)
            {
                label += " | Aktuális hét";
            }

            if (week.IsNextWeek)
            {
                label += " | Jövő hét";
            }

            AvailableWeeks.Add(new WeekOptionViewModel(week.WeekStart, label, week.IsCurrentWeek, week.IsNextWeek));
        }
    }

    private static void ReplaceOptions(ObservableCollection<FoodOptionViewModel> target, IEnumerable<AdminFoodOptionDto> options)
    {
        target.Clear();
        foreach (var option in options.OrderBy(item => item.Name))
        {
            target.Add(new FoodOptionViewModel(option.Id, option.Name));
        }
    }

    private void SelectDefaultWeek()
    {
        var defaultWeek = AvailableWeeks.FirstOrDefault(x => x.IsNextWeek) ?? AvailableWeeks.LastOrDefault();
        if (defaultWeek is null)
        {
            return;
        }

        _isInternalSelectionChange = true;
        SelectedWeek = defaultWeek;
        _isInternalSelectionChange = false;
    }

    private async Task RefreshSelectedWeekAsync()
    {
        if (SelectedWeek is null)
        {
            return;
        }

        if (!_ideiglenesAdatokEngedelyezve)
        {
            await LoadMenuOptionsAsync();
        }

        await LoadWeekAsync(SelectedWeek);
    }

    private async Task LoadWeekAsync(WeekOptionViewModel week)
    {
        try
        {
            SetBusy(isLoading: true);

            if (_ideiglenesAdatokEngedelyezve)
            {
                LoadTemporaryWeek(week);
                return;
            }

            var loadedWeek = await _apiClient.GetMenuWeekAsync(week.WeekStart);
            MenuDays.Clear();

            foreach (var loadedDay in loadedWeek.Days)
            {
                var day = CreateDayEditor(
                    loadedDay.DayName,
                    loadedDay.Date,
                    loadedDay.Plus is not null);

                foreach (var menuDto in loadedDay.Menus)
                {
                    var menu = day.MenuVariants.FirstOrDefault(item => item.MenuCode == menuDto.Code);
                    menu?.SetSelectionsSilently(
                        FindOption(StarterOptions, menuDto.StarterFoodId),
                        FindOption(MainCourseOptions, menuDto.MainCourseFoodId),
                        FindOption(SideDishOptions, menuDto.SideDishFoodId),
                        null);
                }

                if (loadedDay.Plus is not null)
                {
                    var plus = day.MenuVariants.FirstOrDefault(item => item.IsPlusOnly);
                    plus?.SetSelectionsSilently(null, null, null, FindOption(PlusOptions, loadedDay.Plus.FoodId));
                }

                MenuDays.Add(day);
            }

            HasUnsavedChanges = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Az étlap hét betöltése nem sikerült.\n\n{ex.Message}",
                "Étlap",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        finally
        {
            SetBusy(isLoading: false);
        }
    }

    private void LoadTemporaryWeek(WeekOptionViewModel week)
    {
        MenuDays.Clear();

        for (var i = 0; i < 6; i++)
        {
            var date = week.WeekStart.AddDays(i);
            var hasPlus = !week.IsNextWeek && date.DayOfWeek == DayOfWeek.Friday;
            var day = CreateDayEditor(ToHungarianDayName(date), date, hasPlus);

            if (!week.IsNextWeek)
            {
                foreach (var menu in day.MenuVariants)
                {
                    SeedTemporaryVariant(menu);
                }
            }

            MenuDays.Add(day);
        }

        HasUnsavedChanges = false;
    }

    private DailyMenuEditorViewModel CreateDayEditor(string dayName, DateOnly date, bool hasPlus)
    {
        var canHavePlus = date.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday;
        return new DailyMenuEditorViewModel(
            dayName,
            date.ToString("yyyy. MM. dd.", _culture),
            date,
            canHavePlus,
            hasPlus,
            MarkDirty,
            CreateMenuVariant);
    }

    private MenuVariantEditorViewModel CreateMenuVariant(string code)
    {
        var isPlusOnly = code == "PLUS";
        var displayName = isPlusOnly ? "Plusz" : $"{code} menü";

        return new MenuVariantEditorViewModel(
            code,
            displayName,
            GetAccentBrush(code),
            MarkDirty,
            isPlusOnly);
    }

    private void SeedTemporaryVariant(MenuVariantEditorViewModel menu)
    {
        if (menu.IsPlusOnly)
        {
            menu.SetSelectionsSilently(null, null, null, PlusOptions.FirstOrDefault(x => x.Name == "Túrógombóc"));
            return;
        }

        var starter = menu.MenuCode switch
        {
            "A" => StarterOptions.FirstOrDefault(x => x.Name == "Húsleves"),
            "B" => StarterOptions.FirstOrDefault(x => x.Name == "Gulyásleves"),
            "C" => StarterOptions.FirstOrDefault(x => x.Name == "Zöldségleves"),
            _ => StarterOptions.FirstOrDefault()
        };

        var main = menu.MenuCode switch
        {
            "A" => MainCourseOptions.FirstOrDefault(x => x.Name == "Rántott csirkemell"),
            "B" => MainCourseOptions.FirstOrDefault(x => x.Name == "Bakonyi sertésszelet"),
            "C" => MainCourseOptions.FirstOrDefault(x => x.Name == "Rakott karfiol"),
            _ => MainCourseOptions.FirstOrDefault()
        };

        var side = menu.MenuCode switch
        {
            "A" => SideDishOptions.FirstOrDefault(x => x.Name == "Petrezselymes burgonya"),
            "B" => SideDishOptions.FirstOrDefault(x => x.Name == "Párolt rizs"),
            "C" => SideDishOptions.FirstOrDefault(x => x.Name == "Vegyes saláta"),
            _ => SideDishOptions.FirstOrDefault()
        };

        menu.SetSelectionsSilently(starter, main, side, null);
    }

    private async Task SaveMenuAsync()
    {
        if (SelectedWeek is null)
        {
            return;
        }

        if (_ideiglenesAdatokEngedelyezve)
        {
            HasUnsavedChanges = false;
            MessageBox.Show(
                "Az étlap ideiglenes adatokkal működik, ezért a mentés csak a felületen marad meg.",
                "Mentés",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        try
        {
            SetBusy(isSaving: true);
            var request = new AdminSaveMenuWeekRequest
            {
                Days = MenuDays.Select(day => new AdminSaveMenuDayRequest
                {
                    Date = day.Date,
                    Menus = day.MenuVariants
                        .Where(menu => !menu.IsPlusOnly)
                        .Where(menu => menu.SelectedStarter is not null && menu.SelectedMainCourse is not null && menu.SelectedSideDish is not null)
                        .Select(menu => new AdminMenuEditorMenuDto
                        {
                            Code = menu.MenuCode,
                            StarterFoodId = menu.SelectedStarter!.Id,
                            MainCourseFoodId = menu.SelectedMainCourse!.Id,
                            SideDishFoodId = menu.SelectedSideDish!.Id
                        })
                        .ToList(),
                    Plus = day.MenuVariants.FirstOrDefault(menu => menu.IsPlusOnly)?.SelectedPlus is { } plus
                        ? new AdminPlusMenuDto { FoodId = plus.Id }
                        : null
                }).ToList()
            };

            await _apiClient.SaveMenuWeekAsync(SelectedWeek.WeekStart, request);
            await LoadWeekAsync(SelectedWeek);

            MessageBox.Show(
                "Az étlap sikeresen mentve lett a Backenden.",
                "Mentés",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Az étlap mentése nem sikerült.\n\n{ex.Message}",
                "Mentés",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            SetBusy(isSaving: false);
        }
    }

    private void MarkDirty()
    {
        HasUnsavedChanges = true;
    }

    private string ToHungarianDayName(DateOnly date)
    {
        var dayName = _culture.DateTimeFormat.GetDayName(date.DayOfWeek);
        return char.ToUpper(dayName[0], _culture) + dayName[1..];
    }

    private static DateOnly GetWeekStart(DateOnly date)
    {
        var difference = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-difference);
    }

    private static FoodOptionViewModel? FindOption(IEnumerable<FoodOptionViewModel> options, ulong id)
    {
        return id == 0 ? null : options.FirstOrDefault(option => option.Id == id);
    }

    private void SetBusy(bool? isLoading = null, bool? isSaving = null)
    {
        if (isLoading.HasValue)
        {
            _isLoading = isLoading.Value;
        }

        if (isSaving.HasValue)
        {
            _isSaving = isSaving.Value;
        }

        _saveCommand.RaiseCanExecuteChanged();
        _refreshCommand.RaiseCanExecuteChanged();
    }

    private static Brush GetAccentBrush(string menuCode)
    {
        return menuCode switch
        {
            "A" => new SolidColorBrush(Color.FromRgb(92, 122, 168)),
            "B" => new SolidColorBrush(Color.FromRgb(94, 150, 117)),
            "C" => new SolidColorBrush(Color.FromRgb(176, 113, 73)),
            "PLUS" => new SolidColorBrush(Color.FromRgb(160, 109, 180)),
            _ => new SolidColorBrush(Color.FromRgb(120, 120, 120))
        };
    }
}

public sealed class WeekOptionViewModel
{
    public WeekOptionViewModel(DateOnly weekStart, string label, bool isCurrentWeek, bool isNextWeek)
    {
        WeekStart = weekStart;
        Label = label;
        IsCurrentWeek = isCurrentWeek;
        IsNextWeek = isNextWeek;
    }

    public DateOnly WeekStart { get; }

    public string Label { get; }

    public bool IsCurrentWeek { get; }

    public bool IsNextWeek { get; }
}

public sealed class FoodOptionViewModel
{
    public FoodOptionViewModel(ulong id, string name)
    {
        Id = id;
        Name = name;
    }

    public ulong Id { get; }

    public string Name { get; }
}

public sealed class DailyMenuEditorViewModel : ViewModelBase
{
    private readonly Action _onChanged;
    private readonly Func<string, MenuVariantEditorViewModel> _menuFactory;
    private bool _hasPlus;

    public DailyMenuEditorViewModel(
        string dayName,
        string formattedDate,
        DateOnly date,
        bool canTogglePlus,
        bool hasPlus,
        Action onChanged,
        Func<string, MenuVariantEditorViewModel> menuFactory)
    {
        DayName = dayName;
        FormattedDate = formattedDate;
        Date = date;
        CanTogglePlus = canTogglePlus;
        _onChanged = onChanged;
        _menuFactory = menuFactory;
        MenuVariants = [];

        MenuVariants.Add(_menuFactory("A"));
        MenuVariants.Add(_menuFactory("B"));
        MenuVariants.Add(_menuFactory("C"));

        _hasPlus = false;
        if (CanTogglePlus && hasPlus)
        {
            SetPlus(true, markDirty: false);
        }
    }

    public string DayName { get; }

    public string FormattedDate { get; }

    public DateOnly Date { get; }

    public bool CanTogglePlus { get; }

    public ObservableCollection<MenuVariantEditorViewModel> MenuVariants { get; }

    public bool HasPlus
    {
        get => _hasPlus;
        set
        {
            if (!CanTogglePlus || !SetProperty(ref _hasPlus, value))
            {
                return;
            }

            SetPlus(value, markDirty: true);
        }
    }

    private void SetPlus(bool enabled, bool markDirty)
    {
        var existingPlus = MenuVariants.FirstOrDefault(x => x.MenuCode == "PLUS");

        if (enabled && existingPlus is null)
        {
            MenuVariants.Add(_menuFactory("PLUS"));
        }
        else if (!enabled && existingPlus is not null)
        {
            MenuVariants.Remove(existingPlus);
        }

        if (markDirty)
        {
            _onChanged();
        }
    }
}

public sealed class MenuVariantEditorViewModel : ViewModelBase
{
    private readonly Action _onChanged;
    private bool _suspendDirtyState;
    private FoodOptionViewModel? _selectedStarter;
    private FoodOptionViewModel? _selectedMainCourse;
    private FoodOptionViewModel? _selectedSideDish;
    private FoodOptionViewModel? _selectedPlus;

    public MenuVariantEditorViewModel(string menuCode, string displayName, Brush accentBrush, Action onChanged, bool isPlusOnly)
    {
        MenuCode = menuCode;
        DisplayName = displayName;
        AccentBrush = accentBrush;
        _onChanged = onChanged;
        IsPlusOnly = isPlusOnly;
    }

    public string MenuCode { get; }

    public string DisplayName { get; }

    public Brush AccentBrush { get; }

    public bool IsPlusOnly { get; }

    public FoodOptionViewModel? SelectedStarter
    {
        get => _selectedStarter;
        set
        {
            if (SetProperty(ref _selectedStarter, value))
            {
                NotifyChanged();
            }
        }
    }

    public FoodOptionViewModel? SelectedMainCourse
    {
        get => _selectedMainCourse;
        set
        {
            if (SetProperty(ref _selectedMainCourse, value))
            {
                NotifyChanged();
            }
        }
    }

    public FoodOptionViewModel? SelectedSideDish
    {
        get => _selectedSideDish;
        set
        {
            if (SetProperty(ref _selectedSideDish, value))
            {
                NotifyChanged();
            }
        }
    }

    public FoodOptionViewModel? SelectedPlus
    {
        get => _selectedPlus;
        set
        {
            if (SetProperty(ref _selectedPlus, value))
            {
                NotifyChanged();
            }
        }
    }

    public void SetSelectionsSilently(
        FoodOptionViewModel? starter,
        FoodOptionViewModel? mainCourse,
        FoodOptionViewModel? sideDish,
        FoodOptionViewModel? plus)
    {
        _suspendDirtyState = true;
        SelectedStarter = starter;
        SelectedMainCourse = mainCourse;
        SelectedSideDish = sideDish;
        SelectedPlus = plus;
        _suspendDirtyState = false;
    }

    private void NotifyChanged()
    {
        if (_suspendDirtyState)
        {
            return;
        }

        _onChanged();
    }
}
