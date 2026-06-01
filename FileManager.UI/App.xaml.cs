using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using FileManager.Core.Services;
using FileManager.ViewModels;
using FileManager.UI.Services;

namespace FileManager.UI;

/// <summary>
/// Interaction logic for App.xaml
/// Configura la inyección de dependencias para la aplicación.
/// </summary>
public partial class App : Application
{
    private IServiceProvider _serviceProvider;

    public App()
    {
        _serviceProvider = ConfigureServices();
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        LogException("DispatcherUnhandledException", e.Exception);
        e.Handled = false;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            LogException("UnhandledException", ex);
        else
            LogException("UnhandledExceptionObject", new Exception(e.ExceptionObject?.ToString()));
    }

    private void LogException(string source, Exception ex)
    {
        try
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "wpf_error_log.txt");
            var text = $"{DateTime.Now:O} [{source}] {ex}\r\n";
            System.IO.File.AppendAllText(path, text);
        }
        catch { }
    }

    /// <summary>
    /// Configura los servicios de inyección de dependencias.
    /// </summary>
    private IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Registrar servicios
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IPropertiesService, PropertiesService>();

        // Registrar ViewModels
        services.AddSingleton<NavigationViewModel>();
        services.AddSingleton<FileOperationsViewModel>();
        services.AddSingleton<SelectionViewModel>();
        services.AddSingleton<PropertiesViewModel>();
        services.AddSingleton<MainViewModel>();

        // Registrar ventanas
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Obtener la ventana principal del contenedor DI
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        
        // Obtener el ViewModel principal
        var mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();
        
        // Establecer el DataContext
        mainWindow.DataContext = mainViewModel;
        
        // Inicializar el ViewModel de forma asincrónica en background
        _ = mainViewModel.InitializeAsync();
        
        // Mostrar la ventana
        mainWindow.Show();
    }
}

