using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SatisfactoryServerGUI.Services;
using SatisfactoryServerGUI.ViewModels;
using SatisfactoryServerGUI.Views;

namespace SatisfactoryServerGUI;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Services = ConfigureServices();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validation from both Avalonia and CommunityToolkit.
            DisableAvaloniaDataAnnotationValidation();

            var mainVm = Services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow { DataContext = mainVm };
            desktop.ShutdownRequested += (_, _) => mainVm.OnShutdown();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddHttpClient();

        // Services (singletons — they hold live connection/process state for the session).
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ISatisfactoryClientService, SatisfactoryClientService>();
        services.AddSingleton<IServerProcessService, ServerProcessService>();
        services.AddSingleton<ISteamCmdService, SteamCmdService>();
        services.AddSingleton<IConnectionService, ConnectionService>();
        services.AddSingleton<IDialogService, DialogService>();

        // ViewModels (singletons — tabs persist for the app lifetime).
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<ProfilePaneViewModel>();
        services.AddSingleton<AuthViewModel>();
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<ConsoleViewModel>();
        services.AddSingleton<SavesViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<LocalServerViewModel>();

        return services.BuildServiceProvider();
    }

    private static void DisableAvaloniaDataAnnotationValidation()
    {
        var toRemove = BindingPlugins.DataValidators
            .OfType<DataAnnotationsValidationPlugin>()
            .ToArray();

        foreach (var plugin in toRemove)
            BindingPlugins.DataValidators.Remove(plugin);
    }
}
