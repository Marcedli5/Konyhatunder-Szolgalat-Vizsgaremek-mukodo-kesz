using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Windows;
using WPF_AdminFelulet.Services;
using WPF_AdminFelulet.ViewModels;
using WPF_AdminFelulet.Views;

namespace AdminFelulet_WPF;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static IServiceProvider? Services { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        var services = new ServiceCollection();
        var apiConfiguration = AdminApiConfiguration.Load();
        var baseUri = new Uri(apiConfiguration.BaseUrl, UriKind.Absolute);

        services.AddScoped<IViewModelFactory, ViewModelFactory>();
        services.AddSingleton(apiConfiguration);
        services.AddSingleton(new HttpClient
        {
            BaseAddress = baseUri
        });
        services.AddSingleton<IAdminOrdersApiClient, AdminOrdersApiClient>();

        services.AddTransient<EtlapViewModel>();
        services.AddTransient<EtelekViewModel>();
        services.AddTransient<RendelesekAddViewModel>();
        services.AddTransient<RendelesekEditViewModel>();

        services.AddTransient<MainViewModel>();
        services.AddTransient<MainView>();

        Services = services.BuildServiceProvider();

        var window = Services.GetRequiredService<MainView>();
        window.Show();
    }
}
