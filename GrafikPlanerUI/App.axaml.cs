using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GrafikPlanerCore;
using GrafikPlanerUI.Services;
using GrafikPlanerUI.ViewModels;
using GrafikPlanerUI.Views;

namespace GrafikPlanerUI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                new CoreProgram().RunInitializeDatabase();

                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainViewModel(),
                };
            }
            catch (Exception ex)
            {
                StartupErrorHelper.ShowFatalError(ex);
                desktop.Shutdown(1);
                return;
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}