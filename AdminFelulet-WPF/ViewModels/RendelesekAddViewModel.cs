using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using WPF_AdminFelulet.Commands;
using WPF_AdminFelulet.Services;

namespace WPF_AdminFelulet.ViewModels;

public sealed class RendelesekAddViewModel : TabViewModelBase
{
    private bool _ideiglenesAdatokEngedelyezve = false;

    private readonly CultureInfo _culture = new("hu-HU");
    private readonly IAdminOrdersApiClient _apiClient;
    private readonly RelayCommand _addOrderCommand;
    private readonly RelayCommand _clearSessionCommand;
    private readonly RelayCommand _removeSessionOrderCommand;

    private bool _isExistingCustomer = true;
    private OrderCustomerOptionViewModel? _selectedCustomer;
    private OrderMenuOptionViewModel? _selectedMenu;
    private string _quantityText = "1";
    private DateTime? _selectedDeliveryDate;
    private string _comment = string.Empty;
    private string _newCustomerName = string.Empty;
    private string _newCustomerEmail = string.Empty;
    private string _newCustomerPhone = string.Empty;
    private string _newCustomerAddress = string.Empty;
    private bool _isSaving;

    public RendelesekAddViewModel(IAdminOrdersApiClient apiClient)
        : base(
            "Rendelés felvitele",
            "Admin oldali rendelést rögzíthetünk vele úgy, hogy közben a mostani session alatt felvitt tételek külön listában is látszanak.")
    {
        _apiClient = apiClient;

        Customers = [];
        Menus = [];
        SessionOrders = [];

        _addOrderCommand = new RelayCommand(_ => _ = AddOrderAsync(), _ => IsFormValid);
        _clearSessionCommand = new RelayCommand(_ => ClearSessionOrders(), _ => SessionOrders.Count > 0);
        _removeSessionOrderCommand = new RelayCommand(
            parameter => RemoveSessionOrder(parameter as SessionOrderListItemViewModel),
            parameter => parameter is SessionOrderListItemViewModel);

        AddOrderCommand = _addOrderCommand;
        ClearSessionCommand = _clearSessionCommand;
        RemoveSessionOrderCommand = _removeSessionOrderCommand;

        LoadReferenceData();
        SelectedDeliveryDate = DateTime.Today.AddDays(1);
        LoadSessionSeedData();
    }

    public ObservableCollection<OrderCustomerOptionViewModel> Customers { get; }

    public ObservableCollection<OrderMenuOptionViewModel> Menus { get; }

    public ObservableCollection<SessionOrderListItemViewModel> SessionOrders { get; }

    public ICommand AddOrderCommand { get; }

    public ICommand ClearSessionCommand { get; }

    public ICommand RemoveSessionOrderCommand { get; }

    public bool IsExistingCustomer
    {
        get => _isExistingCustomer;
        set
        {
            if (SetProperty(ref _isExistingCustomer, value))
            {
                OnPropertyChanged(nameof(IsNewCustomer));
                RefreshFormState();
            }
        }
    }

    public bool IsNewCustomer => !IsExistingCustomer;

    public OrderCustomerOptionViewModel? SelectedCustomer
    {
        get => _selectedCustomer;
        set
        {
            if (SetProperty(ref _selectedCustomer, value))
            {
                RefreshFormState();
            }
        }
    }

    public OrderMenuOptionViewModel? SelectedMenu
    {
        get => _selectedMenu;
        set
        {
            if (SetProperty(ref _selectedMenu, value))
            {
                RefreshFormState();
            }
        }
    }

    public string QuantityText
    {
        get => _quantityText;
        set
        {
            if (SetProperty(ref _quantityText, value))
            {
                OnPropertyChanged(nameof(QuantityHintText));
                OnPropertyChanged(nameof(OrderPreviewText));
                RefreshFormState();
            }
        }
    }

    public DateTime? SelectedDeliveryDate
    {
        get => _selectedDeliveryDate;
        set
        {
            if (SetProperty(ref _selectedDeliveryDate, value))
            {
                OnPropertyChanged(nameof(SelectedDeliveryDayText));
                OnPropertyChanged(nameof(OrderPreviewText));
                RefreshFormState();
            }
        }
    }

    public string Comment
    {
        get => _comment;
        set => SetProperty(ref _comment, value);
    }

    public string NewCustomerName
    {
        get => _newCustomerName;
        set
        {
            if (SetProperty(ref _newCustomerName, value))
            {
                OnPropertyChanged(nameof(NewCustomerPreviewText));
                RefreshFormState();
            }
        }
    }

    public string NewCustomerEmail
    {
        get => _newCustomerEmail;
        set
        {
            if (SetProperty(ref _newCustomerEmail, value))
            {
                RefreshFormState();
            }
        }
    }

    public string NewCustomerPhone
    {
        get => _newCustomerPhone;
        set => SetProperty(ref _newCustomerPhone, value);
    }

    public string NewCustomerAddress
    {
        get => _newCustomerAddress;
        set
        {
            if (SetProperty(ref _newCustomerAddress, value))
            {
                OnPropertyChanged(nameof(NewCustomerPreviewText));
                RefreshFormState();
            }
        }
    }

    public bool IsFormValid =>
        !IsSaving &&
        HasValidCustomerInput &&
        SelectedMenu is not null &&
        ParseQuantity(QuantityText) > 0 &&
        SelectedDeliveryDate.HasValue;

    public bool IsSaving
    {
        get => _isSaving;
        private set
        {
            if (SetProperty(ref _isSaving, value))
            {
                OnPropertyChanged(nameof(IsFormValid));
                _addOrderCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string QuantityHintText => ParseQuantity(QuantityText) switch
    {
        0 => "Adj meg legalább 1 darab mennyiséget.",
        1 => "1 adag lesz rögzítve.",
        var quantity => $"{quantity} adag lesz rögzítve."
    };

    public string SelectedDeliveryDayText
    {
        get
        {
            if (!SelectedDeliveryDate.HasValue)
            {
                return "Válassz kiszállítási dátumot.";
            }

            var date = DateOnly.FromDateTime(SelectedDeliveryDate.Value);
            return $"{date:yyyy. MM. dd.} - {ToHungarianDayName(date)}";
        }
    }

    public string SessionSummaryText => SessionOrders.Count switch
    {
        0 => "Ebben a sessionben még nincs felvitt rendelés.",
        1 => "1 session rendelés szerepel a listában.",
        _ => $"{SessionOrders.Count} session rendelés szerepel a listában."
    };

    public string OrderPreviewText
    {
        get
        {
            if (!HasValidCustomerInput || SelectedMenu is null || !SelectedDeliveryDate.HasValue)
            {
                return "Válassz ügyfelet, menüt, mennyiséget és dátumot az előzetes összegzéshez.";
            }

            var quantity = Math.Max(ParseQuantity(QuantityText), 1);
            var total = SelectedMenu.UnitPrice * quantity;
            var date = DateOnly.FromDateTime(SelectedDeliveryDate.Value);
            var customerName = IsExistingCustomer
                ? SelectedCustomer?.FullName
                : NewCustomerName.Trim();

            return $"{customerName} részére {quantity} db {SelectedMenu.DisplayName} rögzül {date:yyyy. MM. dd.} ({ToHungarianDayName(date)}) napra. Várható összeg: {total:N0} Ft.";
        }
    }

    public string NewCustomerPreviewText
    {
        get
        {
            if (string.IsNullOrWhiteSpace(NewCustomerName) && string.IsNullOrWhiteSpace(NewCustomerAddress))
            {
                return "Adj meg nevet és lakcímet az új megrendelőhöz.";
            }

            var name = string.IsNullOrWhiteSpace(NewCustomerName) ? "Új megrendelő" : NewCustomerName.Trim();
            var address = string.IsNullOrWhiteSpace(NewCustomerAddress) ? "Lakcím még nem lett kitöltve" : NewCustomerAddress.Trim();
            return $"{name} - {address}";
        }
    }

    private bool HasValidCustomerInput =>
        IsExistingCustomer
            ? SelectedCustomer is not null
            : !string.IsNullOrWhiteSpace(NewCustomerName) &&
              !string.IsNullOrWhiteSpace(NewCustomerEmail) &&
              !string.IsNullOrWhiteSpace(NewCustomerAddress);

    private void LoadReferenceData()
    {
        Customers.Clear();
        Menus.Clear();

        if (_ideiglenesAdatokEngedelyezve)
        {
            LoadTemporaryReferenceData();
            return;
        }

        _ = LoadReferenceDataFromApiAsync();
    }

    private async Task LoadReferenceDataFromApiAsync()
    {
        try
        {
            var users = await _apiClient.GetActiveUsersAsync();
            var menus = await _apiClient.GetActiveMenusAsync();

            foreach (var user in users.OrderBy(user => user.FullName))
            {
                Customers.Add(new OrderCustomerOptionViewModel(
                    user.Id,
                    user.FullName,
                    string.IsNullOrWhiteSpace(user.Address) ? "Nincs cím rögzítve" : user.Address,
                    user.Email,
                    user.Phone));
            }

            foreach (var menu in menus.OrderBy(menu => menu.Code))
            {
                Menus.Add(new OrderMenuOptionViewModel(menu.Id, menu.Code, menu.UnitPrice));
            }
        }
        catch (Exception ex)
        {
            ShowApiError("A rendelésfelvitelhez szükséges törzsadatokat nem sikerült betölteni az API-ból.", ex);
        }

        RefreshFormState();
    }

    private void LoadTemporaryReferenceData()
    {
        Customers.Add(new OrderCustomerOptionViewModel(101, "Kiss Anna", "Szombathely, Kertész utca 12."));
        Customers.Add(new OrderCustomerOptionViewModel(102, "Nagy Péter", "Szombathely, Fő tér 4."));
        Customers.Add(new OrderCustomerOptionViewModel(103, "Tóth Julianna", "Szombathely, Bartók Béla körút 18."));
        Customers.Add(new OrderCustomerOptionViewModel(104, "Varga Zsolt", "Szentkirály, Petőfi Sándor utca 5."));

        Menus.Add(new OrderMenuOptionViewModel(201, "A", 1890));
        Menus.Add(new OrderMenuOptionViewModel(202, "B", 2090));
        Menus.Add(new OrderMenuOptionViewModel(203, "C", 2290));
        Menus.Add(new OrderMenuOptionViewModel(204, "P", 990));
    }

    private void LoadSessionSeedData()
    {
        if (!_ideiglenesAdatokEngedelyezve || Customers.Count == 0 || Menus.Count == 0)
        {
            return;
        }

        SessionOrders.Clear();

        SessionOrders.Add(new SessionOrderListItemViewModel(
            1,
            Customers[0],
            Menus[0],
            2,
            DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            "Kapucsengő hibás, telefonáljon érkezéskor."));

        SessionOrders.Add(new SessionOrderListItemViewModel(
            2,
            Customers[2],
            Menus[1],
            1,
            DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            string.Empty));

        RefreshSessionState();
    }

    private async Task AddOrderAsync()
    {
        if (!IsFormValid || SelectedMenu is null || !SelectedDeliveryDate.HasValue)
        {
            return;
        }

        var selectedMenu = SelectedMenu;
        var quantity = ParseQuantity(QuantityText);
        var deliveryDate = DateOnly.FromDateTime(SelectedDeliveryDate.Value);
        var comment = string.IsNullOrWhiteSpace(Comment) ? null : Comment.Trim();

        try
        {
            IsSaving = true;
            var customer = await ResolveCustomerForOrderAsync();

            if (customer is null)
            {
                return;
            }

            if (!_ideiglenesAdatokEngedelyezve)
            {
                await _apiClient.CreateOrderAsync(new AdminCreateOrderRequest
                {
                    UserId = customer.Id,
                    DeliveryDate = deliveryDate,
                    Comment = comment,
                    Items =
                    [
                        new AdminCreateOrderItemRequest
                        {
                            MenuId = selectedMenu.Id,
                            Quantity = quantity,
                            UnitPrice = selectedMenu.UnitPrice
                        }
                    ]
                });
            }

            var nextLocalId = SessionOrders.Select(item => item.LocalId).DefaultIfEmpty(0).Max() + 1;
            var item = new SessionOrderListItemViewModel(nextLocalId, customer, selectedMenu, quantity, deliveryDate, comment);
            SessionOrders.Insert(0, item);

            ResetForm();
            RefreshSessionState();
        }
        catch (Exception ex)
        {
            ShowApiError("A rendelést nem sikerült menteni az API-n keresztül.", ex);
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void RemoveSessionOrder(SessionOrderListItemViewModel? order)
    {
        if (order is null)
        {
            return;
        }

        SessionOrders.Remove(order);
        RefreshSessionState();
    }

    private void ClearSessionOrders()
    {
        if (SessionOrders.Count == 0)
        {
            return;
        }

        var result = MessageBox.Show(
            "Töröljük az aktuális session listáját? Ez csak a felületi listát üríti ki.",
            "Session lista ürítése",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        SessionOrders.Clear();
        RefreshSessionState();
    }

    private void ResetForm()
    {
        IsExistingCustomer = true;
        SelectedCustomer = null;
        SelectedMenu = null;
        QuantityText = "1";
        SelectedDeliveryDate = DateTime.Today.AddDays(1);
        Comment = string.Empty;
        NewCustomerName = string.Empty;
        NewCustomerEmail = string.Empty;
        NewCustomerPhone = string.Empty;
        NewCustomerAddress = string.Empty;
        RefreshFormState();
    }

    private void RefreshFormState()
    {
        OnPropertyChanged(nameof(IsNewCustomer));
        OnPropertyChanged(nameof(IsFormValid));
        OnPropertyChanged(nameof(QuantityHintText));
        OnPropertyChanged(nameof(SelectedDeliveryDayText));
        OnPropertyChanged(nameof(OrderPreviewText));
        OnPropertyChanged(nameof(NewCustomerPreviewText));
        _addOrderCommand.RaiseCanExecuteChanged();
    }

    private void RefreshSessionState()
    {
        OnPropertyChanged(nameof(SessionSummaryText));
        _clearSessionCommand.RaiseCanExecuteChanged();
    }

    private int ParseQuantity(string rawQuantity)
    {
        return int.TryParse(rawQuantity, out var parsedQuantity) && parsedQuantity > 0
            ? parsedQuantity
            : 0;
    }

    private string ToHungarianDayName(DateOnly date)
    {
        var dayName = _culture.DateTimeFormat.GetDayName(date.DayOfWeek);
        return char.ToUpper(dayName[0], _culture) + dayName[1..];
    }

    private async Task<OrderCustomerOptionViewModel?> ResolveCustomerForOrderAsync()
    {
        if (IsExistingCustomer)
        {
            return SelectedCustomer;
        }

        try
        {
            if (_ideiglenesAdatokEngedelyezve)
            {
                return new OrderCustomerOptionViewModel(
                    0,
                    NewCustomerName.Trim(),
                    NewCustomerAddress.Trim(),
                    NewCustomerEmail.Trim(),
                    string.IsNullOrWhiteSpace(NewCustomerPhone) ? null : NewCustomerPhone.Trim());
            }

            var createdUser = await _apiClient.CreateUserAsync(new AdminCreateUserRequest
            {
                FullName = NewCustomerName.Trim(),
                Email = NewCustomerEmail.Trim(),
                Phone = string.IsNullOrWhiteSpace(NewCustomerPhone) ? null : NewCustomerPhone.Trim(),
                Address = NewCustomerAddress.Trim()
            });

            var customer = new OrderCustomerOptionViewModel(
                createdUser.Id,
                createdUser.FullName,
                string.IsNullOrWhiteSpace(createdUser.Address) ? "Nincs cím rögzítve" : createdUser.Address,
                createdUser.Email,
                createdUser.Phone);

            Customers.Add(customer);
            SelectedCustomer = customer;
            return customer;
        }
        catch (Exception ex)
        {
            ShowApiError("Az új megrendelőt nem sikerült létrehozni az API-n keresztül.", ex);
            return null;
        }
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

public sealed class OrderCustomerOptionViewModel
{
    public OrderCustomerOptionViewModel(ulong id, string fullName, string address, string? email = null, string? phone = null)
    {
        Id = id;
        FullName = fullName;
        Address = address;
        Email = email;
        Phone = phone;
    }

    public ulong Id { get; }

    public string FullName { get; }

    public string Address { get; }

    public string? Email { get; }

    public string? Phone { get; }

    public string DisplayText => $"{FullName} - {Address}";
}

public sealed class OrderMenuOptionViewModel
{
    public OrderMenuOptionViewModel(ulong id, string code, int unitPrice)
    {
        Id = id;
        Code = code;
        UnitPrice = unitPrice;
    }

    public ulong Id { get; }

    public string Code { get; }

    public int UnitPrice { get; }

    public string DisplayName => string.Equals(Code, "P", StringComparison.OrdinalIgnoreCase)
        ? "Plusz menü"
        : $"{Code} menü";

    public string PriceText => $"{UnitPrice:N0} Ft";

    public string DisplayText => $"{DisplayName} - {PriceText}";
}

public sealed class SessionOrderListItemViewModel
{
    public SessionOrderListItemViewModel(
        int localId,
        OrderCustomerOptionViewModel customer,
        OrderMenuOptionViewModel menu,
        int quantity,
        DateOnly deliveryDate,
        string? comment)
    {
        LocalId = localId;
        Customer = customer;
        Menu = menu;
        Quantity = quantity;
        DeliveryDate = deliveryDate;
        Comment = comment;
    }

    public int LocalId { get; }

    public OrderCustomerOptionViewModel Customer { get; }

    public OrderMenuOptionViewModel Menu { get; }

    public int Quantity { get; }

    public DateOnly DeliveryDate { get; }

    public string? Comment { get; }

    public string CustomerName => Customer.FullName;

    public string Address => Customer.Address;

    public string MenuName => Menu.DisplayName;

    public string QuantityText => $"{Quantity} db";

    public string DeliveryDateText => DeliveryDate.ToString("yyyy. MM. dd.");

    public string DeliveryDayName
    {
        get
        {
            var culture = new CultureInfo("hu-HU");
            var dayName = DeliveryDate.ToString("dddd", culture);
            return char.ToUpper(dayName[0], culture) + dayName[1..];
        }
    }

    public string CommentText => string.IsNullOrWhiteSpace(Comment)
        ? "Nincs megjegyzés"
        : Comment!;
}
