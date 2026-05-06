using System.IO;
using System.Text.Json;

namespace WPF_AdminFelulet.Services;

public sealed class AdminApiConfiguration
{
    public string BaseUrl { get; init; } = "http://localhost:5233/";

    public static AdminApiConfiguration Load()
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "apiSettings.json");
        if (!File.Exists(configPath))
        {
            configPath = Path.Combine(Environment.CurrentDirectory, "apiSettings.json");
        }

        if (!File.Exists(configPath))
        {
            return new AdminApiConfiguration();
        }

        try
        {
            var json = File.ReadAllText(configPath);
            var configuration = JsonSerializer.Deserialize<AdminApiConfiguration>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return configuration is not null && !string.IsNullOrWhiteSpace(configuration.BaseUrl)
                ? configuration
                : new AdminApiConfiguration();
        }
        catch
        {
            return new AdminApiConfiguration();
        }
    }
}
