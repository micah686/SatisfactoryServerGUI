using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SatisfactoryServerGUI.Services;
using SatisfactoryServerGUI.ViewModels;
using SatisfactoryServerGUI.Views;
using System;

namespace SatisfactoryServerGUI;

public partial class App : Application
{
    public new static App Current => (App)Application.Current!;
    public IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Services
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IServerProcessService, ServerProcessService>();
        services.AddSingleton<ISatisfactoryClientService, SatisfactoryClientService>();

        // ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<ServerControlsViewModel>();
        services.AddTransient<ServerDetailsViewModel>();
        services.AddTransient<ServerFoldersViewModel>();
        services.AddTransient<ServerSettingsViewModel>();
        services.AddTransient<ConsoleLogsViewModel>();
    }
}
