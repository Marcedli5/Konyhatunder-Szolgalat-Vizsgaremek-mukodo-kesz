using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using WPF_AdminFelulet.Commands;
using WPF_AdminFelulet.Services;

namespace WPF_AdminFelulet.ViewModels;

public sealed class RendelesekEditViewModel : TabViewModelBase
{
    private bool _ideiglenesAdatokEngedelyezve = false;

    private readonly CultureInfo _culture = new("hu-HU");
    private readonly IAdminOrdersApiClient _apiClient;
    private readonly RelayCommand _refreshCommand;
    private readonly RelayCommand _saveChangesCommand;
    private readonly RelayCommand _deleteOrderCommand;
    private readonly RelayCommand _clearSelectionCommand;
    private readonly List<EditableOrderListItemViewModel> _allOrders = [];

    private string _searchText = string.Empty;
    private EditableOrderListItemViewModel? _selectedOrder;
    private OrderMenuOptionViewModel? _selectedEditMenu;
    private string _editedCustomerName = string.Empty;
    private string _editedAddress = string.Empty;
    private string _editedPhone = string.Empty;
    private string _editedEmail = string.Empty;
    private string _editedQuantityText = "1";
    private DateTime? _editedDeliveryDate;
    private string _editedComment = string.Empty;
    private bool _isLoading;
    private bool _isSaving;

    public RendelesekEditViewModel(IAdminOrdersApiClient apiClient)
        : base(
            "Rendelések szerkesztése",
            "A rendeléslista csak a múlt heti, aktuális heti, következő heti, valamint ha létezik, az azt követő heti tételeket mutatja. A keresés név vagy lakcím alapján azonnal szűr.")
    {
        _apiClient = apiClient;

        Menus = [];
        FilteredOrders = [];

        _refreshCommand = new RelayCommand(_ => _ = ReloadOrdersAsync(), _ => !_isLoading && !_isSaving);
        _saveChangesCommand = new RelayCommand(_ => _ = SaveChangesAsync(), _ => CanSaveChanges);
        _deleteOrderCommand = new RelayCommand(_ => _ = DeleteSelectedOrderAsync(), _ => CanDeleteSelectedOrder);
        _clearSelectionCommand = new RelayCommand(_ => ClearSelection(), _ => SelectedOrder is not null);

        RefreshCommand = _refreshCommand;
        SaveChangesCommand = _saveChangesCommand;
        DeleteOrderCommand = _deleteOrderCommand;
        ClearSelectionCommand = _clearSelectionCommand;

        _ = ReloadOrdersAsync();
    }

    public ObservableCollection<OrderMenuOptionViewModel> Menus { get; }

    public ObservableCollection<EditableOrderListItemViewModel> FilteredOrders { get; }

    public ICommand RefreshCommand { get; }

    public ICommand SaveChangesCommand { get; }

    public ICommand DeleteOrderCommand { get; }

    public ICommand ClearSelectionCommand { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilters();
                OnPropertyChanged(nameof(SearchSummaryText));
            }
        }
    }

    public EditableOrderListItemViewModel? SelectedOrder
    {
        get => _selectedOrder;
        set
        {
            if (SetProperty(ref _selectedOrder, value))
            {
                PopulateEditorFromSelection(value);
                OnPropertyChanged(nameof(HasSelectedOrder));
                OnPropertyChanged(nameof(EditorHeaderText));
                OnPropertyChanged(nameof(EditorSubText));
                RefreshEditorState();
                _deleteOrderCommand.RaiseCanExecuteChanged();
                _clearSelectionCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public OrderMenuOptionViewModel? SelectedEditMenu
    {
        get => _selectedEditMenu;
        set
        {
            if (SetProperty(ref _selectedEditMenu, value))
            {
                OnPropertyChanged(nameof(EditorTotalText));
                OnPropertyChanged(nameof(EditorSummaryText));
                RefreshEditorState();
            }
        }
    }

    public string EditedCustomerName
    {
        get => _editedCustomerName;
        set
        {
            if (SetProperty(ref _editedCustomerName, value))
            {
                OnPropertyChanged(nameof(EditorSummaryText));
                RefreshEditorState();
            }
        }
    }

    public string EditedAddress
    {
        get => _editedAddress;
        set
        {
            if (SetProperty(ref _editedAddress, value))
            {
                RefreshEditorState();
            }
        }
    }

    public string EditedPhone
    {
        get => _editedPhone;
        set => SetProperty(ref _editedPhone, value);
    }

    public string EditedEmail
    {
        get => _editedEmail;
        set
        {
            if (SetProperty(ref _editedEmail, value))
            {
                RefreshEditorState();
            }
        }
    }

    public string EditedQuantityText
    {
        get => _editedQuantityText;
        set
        {
            if (SetProperty(ref _editedQuantityText, value))
            {
                OnPropertyChanged(nameof(EditorTotalText));
                OnPropertyChanged(nameof(EditorSummaryText));
                RefreshEditorState();
            }
        }
    }

    public DateTime? EditedDeliveryDate
    {
        get => _editedDeliveryDate;
        set
        {
            if (SetProperty(ref _editedDeliveryDate, value))
            {
                OnPropertyChanged(nameof(EditedDeliveryDateText));
                OnPropertyChanged(nameof(EditorSummaryText));
                RefreshEditorState();
            }
        }
    }

    public string EditedComment
    {
        get => _editedComment;
        set => SetProperty(ref _editedComment, value);
    }

    public bool HasSelectedOrder => SelectedOrder is not null;

    public bool CanSaveChanges =>
        SelectedOrder is not null &&
        !_isLoading &&
        !_isSaving &&
        !string.IsNullOrWhiteSpace(EditedCustomerName) &&
        !string.IsNullOrWhiteSpace(EditedAddress) &&
        !string.IsNullOrWhiteSpace(EditedEmail) &&
        SelectedEditMenu is not null &&
        EditedDeliveryDate.HasValue &&
        ParseQuantity(EditedQuantityText) > 0;

    public bool CanDeleteSelectedOrder => SelectedOrder is not null && !_isLoading && !_isSaving;

    public string SearchSummaryText =>
        string.IsNullOrWhiteSpace(SearchText)
            ? "A lista név vagy lakcím alapján szűrhető."
            : $"Aktív keresés: \"{SearchText.Trim()}\"";

    public string VisibleOrdersCountText => FilteredOrders.Count switch
    {
        0 => "Nincs a feltételeknek megfelelő rendelés a listában.",
        1 => "1 rendelés látszik a listában.",
        _ => $"{FilteredOrders.Count} rendelés látszik a listában."
    };

    public int VisibleEstimatedTotal => FilteredOrders.Sum(order => order.TotalPrice);

    public string VisibleEstimatedTotalText => $"{VisibleEstimatedTotal:N0} Ft";

    public int VisibleAMenuCount => FilteredOrders.Count(order => string.Equals(order.MenuCode, "A", StringComparison.OrdinalIgnoreCase));

    public int VisibleBMenuCount => FilteredOrders.Count(order => string.Equals(order.MenuCode, "B", StringComparison.OrdinalIgnoreCase));

    public int VisibleCMenuCount => FilteredOrders.Count(order => string.Equals(order.MenuCode, "C", StringComparison.OrdinalIgnoreCase));

    public string EditorHeaderText => SelectedOrder is null
        ? "Kijelölt rendelés szerkesztése"
        : $"Szerkesztés alatt: {SelectedOrder.CustomerName}";

    public string EditorSubText => SelectedOrder is null
        ? "Válassz ki egy sort a jobb oldali listából, és itt azonnal módosítani tudod az adatokat."
        : $"Rendelés azonosító: #{SelectedOrder.OrderId} / menü: {SelectedOrder.MenuName}";

    public string EditedDeliveryDateText
    {
        get
        {
            if (!EditedDeliveryDate.HasValue)
            {
                return "Válassz kiszállítási dátumot.";
            }

            var date = DateOnly.FromDateTime(EditedDeliveryDate.Value);
            return $"{date:yyyy. MM. dd.} - {ToHungarianDayName(date)}";
        }
    }

    public string EditorTotalText
    {
        get
        {
            var quantity = ParseQuantity(EditedQuantityText);
            if (SelectedEditMenu is null || quantity <= 0)
            {
                return "Összesen: 0 Ft";
            }

            return $"Összesen: {SelectedEditMenu.UnitPrice * quantity:N0} Ft";
        }
    }

    public string EditorSummaryText
    {
        get
        {
            if (!CanSaveChanges)
            {
                return "Töltsd ki a kötelező megrendelő, menü, mennyiség és dátum mezőket a mentéshez.";
            }

            var quantity = ParseQuantity(EditedQuantityText);
            var deliveryDate = DateOnly.FromDateTime(EditedDeliveryDate!.Value);
            return $"{EditedCustomerName.Trim()} részére {quantity} db {SelectedEditMenu!.DisplayName} lesz rögzítve {deliveryDate:yyyy. MM. dd.} napra. {EditorTotalText}";
        }
    }

    private async Task ReloadOrdersAsync(ulong? selectedOrderId = null, ulong? selectedMenuId = null)
    {
        selectedOrderId ??= SelectedOrder?.OrderId;
        selectedMenuId ??= SelectedOrder?.MenuId;

        SetBusy(isLoading: true);
        try
        {
            Menus.Clear();
            _allOrders.Clear();

            var currentWeekStart = StartOfWeek(DateOnly.FromDateTime(DateTime.Today));
            var previousWeekStart = currentWeekStart.AddDays(-7);
            var weekAfterNextStart = currentWeekStart.AddDays(14);
            var weekAfterNextEndExclusive = currentWeekStart.AddDays(21);

            if (_ideiglenesAdatokEngedelyezve)
            {
                LoadTemporaryData(previousWeekStart, weekAfterNextStart, weekAfterNextEndExclusive);
                ApplyFilters();
                RestoreSelection(selectedOrderId, selectedMenuId);
                return;
            }

            await ReloadOrdersFromApiAsync(
                selectedOrderId,
                selectedMenuId,
                previousWeekStart,
                weekAfterNextStart,
                weekAfterNextEndExclusive);
        }
        finally
        {
            SetBusy(isLoading: false);
        }
    }

    private async Task ReloadOrdersFromApiAsync(
        ulong? selectedOrderId,
        ulong? selectedMenuId,
        DateOnly previousWeekStart,
        DateOnly weekAfterNextStart,
        DateOnly weekAfterNextEndExclusive)
    {
        try
        {
            var menus = await _apiClient.GetActiveMenusAsync();
            foreach (var menu in menus.OrderBy(menu => menu.Code))
            {
                Menus.Add(new OrderMenuOptionViewModel(menu.Id, menu.Code, menu.UnitPrice));
            }

            var orders = await _apiClient.GetOrdersAsync(previousWeekStart, weekAfterNextEndExclusive.AddDays(-1), SearchText);

            var hasFourthWeekOrders = orders.Any(order =>
                order.DeliveryDate >= weekAfterNextStart &&
                order.DeliveryDate < weekAfterNextEndExclusive);

            var visibleEndExclusive = hasFourthWeekOrders ? weekAfterNextEndExclusive : weekAfterNextStart;

            foreach (var order in orders.OrderBy(order => order.DeliveryDate).ThenBy(order => order.CustomerName))
            {
                if (order.DeliveryDate < previousWeekStart || order.DeliveryDate >= visibleEndExclusive)
                {
                    continue;
                }

                _allOrders.Add(new EditableOrderListItemViewModel(
                    order.OrderId,
                    order.MenuId,
                    order.UserId,
                    order.CustomerName,
                    order.Address,
                    order.Phone,
                    order.Email,
                    order.DeliveryDate,
                    order.MenuCode,
                    order.Quantity,
                    order.UnitPrice,
                    order.Comment,
                    false));
            }
        }
        catch (Exception ex)
        {
            ShowApiError("A rendeléslista nem sikerült az API-ból betölteni.", ex);
        }

        ApplyFilters();
        RestoreSelection(selectedOrderId, selectedMenuId);
        OnPropertyChanged(nameof(VisibleOrdersCountText));
    }

    private void LoadTemporaryData(
        DateOnly previousWeekStart,
        DateOnly weekAfterNextStart,
        DateOnly weekAfterNextEndExclusive)
    {
        Menus.Clear();
        _allOrders.Clear();

        var tempMenus = new[]
        {
            new OrderMenuOptionViewModel(201, "A", 1890),
            new OrderMenuOptionViewModel(202, "B", 2090),
            new OrderMenuOptionViewModel(203, "C", 2290)
        };

        foreach (var menu in tempMenus)
        {
            Menus.Add(menu);
        }

        var tempOrders = new List<EditableOrderListItemViewModel>
        {
            new(9001, 201, 101, "Kiss Anna", "Szombathely, Kertész utca 12.", "06301234567", "anna@example.com", previousWeekStart.AddDays(2), "A", 2, 1890, "Érkezés előtt telefonáljon.", true),
            new(9002, 202, 102, "Nagy Péter", "Szombathely, Fő tér 4.", "06305551212", "peter@example.com", previousWeekStart.AddDays(9), "B", 1, 2090, string.Empty, true),
            new(9003, 203, 103, "Tóth Julianna", "Szombathely, Bartók Béla körút 18.", "06307778888", "juli@example.com", previousWeekStart.AddDays(16), "C", 3, 2290, "A kapucsengő nem működik.", true),
            new(9004, 201, 104, "Varga Zsolt", "Szentkirály, Petőfi Sándor utca 5.", "06306667777", "zsolt@example.com", weekAfterNextStart.AddDays(1), "A", 1, 1890, "Ha lehet, délután érkezzen.", true)
        };

        var hasFourthWeekOrders = tempOrders.Any(order => order.DeliveryDate >= weekAfterNextStart && order.DeliveryDate < weekAfterNextEndExclusive);
        var visibleRangeEndExclusive = hasFourthWeekOrders ? weekAfterNextEndExclusive : weekAfterNextStart;

        foreach (var order in tempOrders)
        {
            if (order.DeliveryDate < previousWeekStart || order.DeliveryDate >= visibleRangeEndExclusive)
            {
                continue;
            }

            _allOrders.Add(order);
        }
    }

    private void ApplyFilters()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allOrders
            : _allOrders
                .Where(order =>
                    order.CustomerName.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase) ||
                    order.Address.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();

        FilteredOrders.Clear();

        foreach (var order in filtered
                     .OrderBy(order => order.DeliveryDate)
                     .ThenBy(order => order.CustomerName)
                     .ThenBy(order => order.MenuCode))
        {
            FilteredOrders.Add(order);
        }

        OnPropertyChanged(nameof(VisibleOrdersCountText));
        OnPropertyChanged(nameof(VisibleEstimatedTotal));
        OnPropertyChanged(nameof(VisibleEstimatedTotalText));
        OnPropertyChanged(nameof(VisibleAMenuCount));
        OnPropertyChanged(nameof(VisibleBMenuCount));
        OnPropertyChanged(nameof(VisibleCMenuCount));
    }

    private void RestoreSelection(ulong? orderId, ulong? menuId)
    {
        if (!orderId.HasValue || !menuId.HasValue)
        {
            if (SelectedOrder is not null && !_allOrders.Contains(SelectedOrder))
            {
                SelectedOrder = null;
            }

            return;
        }

        var restored = _allOrders.FirstOrDefault(order => order.OrderId == orderId.Value && order.MenuId == menuId.Value);
        if (restored is not null)
        {
            SelectedOrder = restored;
        }
        else if (SelectedOrder is not null)
        {
            SelectedOrder = null;
        }
    }

    private void PopulateEditorFromSelection(EditableOrderListItemViewModel? order)
    {
        if (order is null)
        {
            _editedCustomerName = string.Empty;
            _editedAddress = string.Empty;
            _editedPhone = string.Empty;
            _editedEmail = string.Empty;
            _editedQuantityText = "1";
            _editedDeliveryDate = null;
            _editedComment = string.Empty;
            _selectedEditMenu = null;
        }
        else
        {
            _editedCustomerName = order.CustomerName;
            _editedAddress = order.Address;
            _editedPhone = order.Phone ?? string.Empty;
            _editedEmail = order.Email;
            _editedQuantityText = order.Quantity.ToString(_culture);
            _editedDeliveryDate = order.DeliveryDate.ToDateTime(TimeOnly.MinValue);
            _editedComment = order.Comment ?? string.Empty;
            _selectedEditMenu = Menus.FirstOrDefault(menu => menu.Id == order.MenuId)
                ?? new OrderMenuOptionViewModel(order.MenuId, order.MenuCode, order.UnitPrice);
        }

        OnPropertyChanged(nameof(EditedCustomerName));
        OnPropertyChanged(nameof(EditedAddress));
        OnPropertyChanged(nameof(EditedPhone));
        OnPropertyChanged(nameof(EditedEmail));
        OnPropertyChanged(nameof(EditedQuantityText));
        OnPropertyChanged(nameof(EditedDeliveryDate));
        OnPropertyChanged(nameof(EditedComment));
        OnPropertyChanged(nameof(SelectedEditMenu));
        OnPropertyChanged(nameof(EditedDeliveryDateText));
        OnPropertyChanged(nameof(EditorTotalText));
        OnPropertyChanged(nameof(EditorSummaryText));
    }

    private async Task SaveChangesAsync()
    {
        if (!CanSaveChanges || SelectedOrder is null || SelectedEditMenu is null || !EditedDeliveryDate.HasValue)
        {
            return;
        }

        var selectedOrder = SelectedOrder;
        var selectedMenu = SelectedEditMenu;
        var quantity = ParseQuantity(EditedQuantityText);
        var trimmedName = EditedCustomerName.Trim();
        var trimmedAddress = EditedAddress.Trim();
        var trimmedPhone = string.IsNullOrWhiteSpace(EditedPhone) ? null : EditedPhone.Trim();
        var trimmedEmail = EditedEmail.Trim();
        var deliveryDate = DateOnly.FromDateTime(EditedDeliveryDate.Value);
        var trimmedComment = string.IsNullOrWhiteSpace(EditedComment) ? null : EditedComment.Trim();
        var originalOrderId = selectedOrder.OrderId;
        var originalMenuId = selectedOrder.MenuId;
        var updatedMenuId = selectedMenu.Id;

        if (selectedOrder.IsTemporary)
        {
            selectedOrder.Update(
                trimmedName,
                trimmedAddress,
                trimmedPhone,
                trimmedEmail,
                deliveryDate,
                selectedMenu.Id,
                selectedMenu.Code,
                quantity,
                selectedMenu.UnitPrice,
                trimmedComment);

            ApplyFilters();
            SelectCurrentEditedOrder(originalOrderId, updatedMenuId);
            return;
        }

        try
        {
            SetBusy(isSaving: true);

            await _apiClient.UpdateOrderItemAsync(originalOrderId, originalMenuId, new AdminUpdateOrderItemRequest
            {
                Customer = new AdminOrderCustomerUpdateDto
                {
                    FullName = trimmedName,
                    Address = trimmedAddress,
                    Phone = trimmedPhone,
                    Email = trimmedEmail
                },
                DeliveryDate = deliveryDate,
                Comment = trimmedComment,
                MenuId = selectedMenu.Id,
                Quantity = quantity,
                UnitPrice = selectedMenu.UnitPrice
            });

            await ReloadOrdersAsync(originalOrderId, updatedMenuId);
        }
        catch (AdminApiException ex) when (string.Equals(ex.Code, "duplicate_menu_item", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(
                "Ehhez a rendeléshez már tartozik ilyen menü. Válassz másik menüt, vagy töröld a másik sort.",
                "Menü ütközés",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            ShowApiError("A rendelés módosítását nem sikerült elmenteni az API-n keresztül.", ex);
        }
        finally
        {
            SetBusy(isSaving: false);
        }
    }

    private async Task DeleteSelectedOrderAsync()
    {
        if (SelectedOrder is null)
        {
            return;
        }

        var result = MessageBox.Show(
            $"Biztosan töröljük a kijelölt rendelést? ({SelectedOrder.CustomerName} - {SelectedOrder.MenuName})",
            "Rendelés törlése",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        if (SelectedOrder.IsTemporary)
        {
            _allOrders.Remove(SelectedOrder);
            SelectedOrder = null;
            ApplyFilters();
            return;
        }

        try
        {
            SetBusy(isSaving: true);

            await _apiClient.DeleteOrderItemAsync(SelectedOrder.OrderId, SelectedOrder.MenuId);
            SelectedOrder = null;
            await ReloadOrdersAsync();
        }
        catch (Exception ex)
        {
            ShowApiError("A rendelés törlése nem sikerült az API-n keresztül.", ex);
        }
        finally
        {
            SetBusy(isSaving: false);
        }
    }

    private void ClearSelection()
    {
        SelectedOrder = null;
    }

    private void SelectCurrentEditedOrder(ulong orderId, ulong menuId)
    {
        var updatedSelection = _allOrders.FirstOrDefault(order => order.OrderId == orderId && order.MenuId == menuId);
        if (updatedSelection is not null)
        {
            SelectedOrder = updatedSelection;
        }
    }

    private void RefreshEditorState()
    {
        OnPropertyChanged(nameof(CanSaveChanges));
        OnPropertyChanged(nameof(CanDeleteSelectedOrder));
        OnPropertyChanged(nameof(EditedDeliveryDateText));
        OnPropertyChanged(nameof(EditorTotalText));
        OnPropertyChanged(nameof(EditorSummaryText));
        _saveChangesCommand.RaiseCanExecuteChanged();
        _deleteOrderCommand.RaiseCanExecuteChanged();
        _clearSelectionCommand.RaiseCanExecuteChanged();
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

        _refreshCommand.RaiseCanExecuteChanged();
        RefreshEditorState();
    }

    private int ParseQuantity(string rawQuantity)
    {
        return int.TryParse(rawQuantity, out var parsedQuantity) && parsedQuantity > 0
            ? parsedQuantity
            : 0;
    }

    private DateOnly StartOfWeek(DateOnly date)
    {
        var offset = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-offset);
    }

    private string ToHungarianDayName(DateOnly date)
    {
        var dayName = _culture.DateTimeFormat.GetDayName(date.DayOfWeek);
        return char.ToUpper(dayName[0], _culture) + dayName[1..];
    }

    private static void ShowApiError(string prefix, Exception exception)
    {
        var details = exception is AdminApiException apiException
            ? apiException.Message
            : exception.Message;

        MessageBox.Show(
            $"{prefix}\n\n{details}",
            "API hiba",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}

public sealed class EditableOrderListItemViewModel
{
    private readonly CultureInfo _culture = new("hu-HU");

    public EditableOrderListItemViewModel(
        ulong orderId,
        ulong menuId,
        ulong customerId,
        string customerName,
        string? address,
        string? phone,
        string? email,
        DateOnly deliveryDate,
        string menuCode,
        int quantity,
        int unitPrice,
        string? comment,
        bool isTemporary)
    {
        OrderId = orderId;
        MenuId = menuId;
        CustomerId = customerId;
        CustomerName = string.IsNullOrWhiteSpace(customerName) ? "Ismeretlen megrendelő" : customerName;
        Address = string.IsNullOrWhiteSpace(address) ? "Nincs rögzített lakcím" : address;
        Phone = phone;
        Email = string.IsNullOrWhiteSpace(email) ? "Nincs e-mail-cím" : email;
        DeliveryDate = deliveryDate;
        MenuCode = menuCode;
        Quantity = quantity;
        UnitPrice = unitPrice;
        Comment = comment;
        IsTemporary = isTemporary;
    }

    public ulong OrderId { get; }

    public ulong MenuId { get; private set; }

    public ulong CustomerId { get; }

    public string CustomerName { get; private set; }

    public string Address { get; private set; }

    public string? Phone { get; private set; }

    public string Email { get; private set; }

    public DateOnly DeliveryDate { get; private set; }

    public string MenuCode { get; private set; }

    public int Quantity { get; private set; }

    public int UnitPrice { get; private set; }

    public string? Comment { get; private set; }

    public bool IsTemporary { get; }

    public string MenuName => $"{MenuCode} menü";

    public string DeliveryDateText => DeliveryDate.ToString("yyyy. MM. dd.", _culture);

    public string DeliveryDayText
    {
        get
        {
            var dayName = DeliveryDate.ToString("dddd", _culture);
            return char.ToUpper(dayName[0], _culture) + dayName[1..];
        }
    }

    public string QuantityText => $"{Quantity} db";

    public int TotalPrice => Quantity * UnitPrice;

    public string TotalPriceText => $"{TotalPrice:N0} Ft";

    public string PhoneText => string.IsNullOrWhiteSpace(Phone) ? "Nincs telefonszám" : Phone!;

    public string CommentText => string.IsNullOrWhiteSpace(Comment) ? "Nincs megjegyzés" : Comment!;

    public string MenuAccentBrush => MenuCode.ToUpperInvariant() switch
    {
        "A" => "#FFE8F1FF",
        "B" => "#FFFDEFD9",
        "C" => "#FFEAF7EE",
        _ => "#FFF8FBFF"
    };

    public string MenuAccentLineBrush => MenuCode.ToUpperInvariant() switch
    {
        "A" => "#FF9FBDE6",
        "B" => "#FFE0BE7A",
        "C" => "#FFA9D0B5",
        _ => "#FFD9E3F0"
    };

    public void Update(
        string customerName,
        string address,
        string? phone,
        string email,
        DateOnly deliveryDate,
        ulong menuId,
        string menuCode,
        int quantity,
        int unitPrice,
        string? comment)
    {
        CustomerName = customerName;
        Address = address;
        Phone = phone;
        Email = email;
        DeliveryDate = deliveryDate;
        MenuId = menuId;
        MenuCode = menuCode;
        Quantity = quantity;
        UnitPrice = unitPrice;
        Comment = comment;
    }
}
