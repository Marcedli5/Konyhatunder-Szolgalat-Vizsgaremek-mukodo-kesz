using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WPF_AdminFelulet.Commands;
using WPF_AdminFelulet.Services;
using WPF_AdminFelulet.Views;

namespace WPF_AdminFelulet.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly IAdminOrdersApiClient _apiClient;
    private TabViewModelBase? _selectedTab;

    public MainViewModel(IViewModelFactory factory, IAdminOrdersApiClient apiClient)
    {
        _apiClient = apiClient;
        Tabs =
        [
            factory.Create<EtlapViewModel>(),
            factory.Create<EtelekViewModel>(),
            factory.Create<RendelesekAddViewModel>(),
            factory.Create<RendelesekEditViewModel>()
        ];

        ReportIssueCommand = new RelayCommand(_ => _ = OpenReportIssueDialogAsync());
        SelectedTab = Tabs.FirstOrDefault();
    }

    public ObservableCollection<TabViewModelBase> Tabs { get; }

    public ICommand ReportIssueCommand { get; }

    public TabViewModelBase? SelectedTab
    {
        get => _selectedTab;
        set
        {
            if (SetProperty(ref _selectedTab, value))
            {
                OnPropertyChanged(nameof(SelectedTabTitle));
                OnPropertyChanged(nameof(SelectedTabDescription));
            }
        }
    }

    public string SelectedTabTitle => SelectedTab?.Title ?? "Konyhatündér admin";

    public string SelectedTabDescription => SelectedTab?.Description ?? "Egységes adminisztrációs felület étlapokhoz és rendelésekhez.";

    private async Task OpenReportIssueDialogAsync()
    {
        var dialogViewModel = new IssueReportDialogViewModel();
        var dialog = new ReportIssueWindow(dialogViewModel)
        {
            Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(window => window.IsActive)
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            await PersistIssueAsync(dialogViewModel.Description.Trim());

            MessageBox.Show(
                "A hibajelentés sikeresen rögzült WPF-issue ticketként.",
                "Hibajelentés elküldve",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(
                ex.Message,
                "Hibajelentés",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch (Exception)
        {
            MessageBox.Show(
                "A hibajelentés mentése közben hiba történt.",
                "Hibajelentés",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private Task PersistIssueAsync(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new InvalidOperationException("A hibajelentés szövege nem lehet üres.");
        }

        return _apiClient.CreateTicketAsync(new AdminCreateTicketRequest
        {
            TicketTypeName = "WPF-issue",
            Description = description,
            Customer = new AdminCreateUserRequest
            {
                FullName = "WPF-Admin",
                Email = "wpf-admin@local",
                Phone = "000000000",
                Address = "AdminFelulet"
            }
        });
    }
}
